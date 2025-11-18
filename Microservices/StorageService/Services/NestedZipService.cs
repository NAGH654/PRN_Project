using System.IO.Compression;
using System.Security.Cryptography;
using StorageService.Data;
using StorageService.Entities;
using StorageService.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace StorageService.Services;

public class NestedZipService : INestedZipService
{
    private readonly StorageDbContext _context;
    private readonly ILogger<NestedZipService> _logger;
    private readonly string _storagePath;
    private static readonly Regex StudentIdRegex = new Regex(@"^[A-Z]{2}\d{6,10}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public NestedZipService(StorageDbContext context, ILogger<NestedZipService> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _storagePath = configuration["Storage:BasePath"] ?? Path.Combine(AppContext.BaseDirectory, "storage");
    }

    public async Task<ProcessingResult> ProcessNestedZipArchiveAsync(UploadBatchForm form, CancellationToken cancellationToken = default)
    {
        try
        {
            if (form.Archive == null || form.Archive.Length == 0)
            {
                throw new ArgumentException("Archive file is required and cannot be empty.");
            }

            var jobId = Guid.NewGuid().ToString();
            var uploadsDir = EnsureDirectory(Path.Combine(_storagePath, "uploads", DateTime.UtcNow.ToString("yyyyMMddHHmmss")));
            var jobsDir = EnsureDirectory(Path.Combine(_storagePath, "jobs", jobId));
            var extractDir = EnsureDirectory(Path.Combine(jobsDir, "extract"));

            var uploadPath = Path.Combine(uploadsDir, SanitizeFileName(form.Archive.FileName));
            _logger.LogInformation("Uploading file: {FileName}, Size: {Size} bytes", form.Archive.FileName, form.Archive.Length);

            await using (var fs = File.Create(uploadPath))
            {
                await form.Archive.CopyToAsync(fs, cancellationToken);
            }
            _logger.LogInformation("File saved to: {UploadPath}", uploadPath);

            _logger.LogInformation("Starting archive extraction...");
            await ExtractArchiveAsync(uploadPath, extractDir, cancellationToken);
            _logger.LogInformation("Archive extraction completed. Extract directory: {ExtractDir}", extractDir);

            var result = new ProcessingResult
            {
                JobId = jobId,
                UploadPath = uploadPath,
                ExtractPath = extractDir
            };

            // Step 1: Extract all nested solution.zip files
            _logger.LogInformation("Searching for nested solution.zip files...");
            await ExtractAllNestedSolutionZipAsync(extractDir, cancellationToken);

            // Step 2: Find DOCX files from extracted directories
            var docxFiles = new List<string>();
            var extractedZipDirs = Directory.GetDirectories(extractDir, "solution_extracted", SearchOption.AllDirectories);
            foreach (var zipDir in extractedZipDirs)
            {
                docxFiles.AddRange(Directory.GetFiles(zipDir, "*.docx", SearchOption.AllDirectories));
            }
            docxFiles.AddRange(Directory.GetFiles(extractDir, "*.docx", SearchOption.AllDirectories));
            docxFiles = docxFiles.Distinct().ToList();

            result.TotalFiles = docxFiles.Count;
            _logger.LogInformation("Found {Count} DOCX files to process", docxFiles.Count);

            // Step 3: Get existing file hashes to detect duplicates
            var sessionIdGuid = Guid.Parse(form.SessionId!);
            var existingFiles = await _context.SubmissionFiles
                .Where(f => f.Submission.ExamSessionId == sessionIdGuid && f.FileHash != null)
                .Select(f => f.FileHash!)
                .ToListAsync(cancellationToken);
            var batchHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Step 4: Process each DOCX file
            var submissionFiles = new List<SubmissionFile>();
            foreach (var file in docxFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var fileName = Path.GetFileName(file);
                    var (studentId, studentName) = ParseStudentInfo(fileName);
                    var hash = await ComputeSha256Async(file, cancellationToken);
                    var isDuplicate = (hash != null && (existingFiles.Contains(hash) || !batchHashes.Add(hash)));

                    if (isDuplicate)
                    {
                        result.DuplicateFiles++;
                        _logger.LogWarning("Duplicate file detected: {FileName}", fileName);
                        continue;
                    }

                    // Create submission record
                    var submission = new Submission
                    {
                        Id = Guid.NewGuid(),
                        StudentId = Guid.Empty, // Will need to lookup from Identity service
                        ExamId = Guid.Empty, // Will need to lookup from session
                        ExamSessionId = sessionIdGuid,
                        Status = "Completed",
                        SubmittedAt = DateTime.UtcNow,
                        ProcessedAt = DateTime.UtcNow,
                        TotalFiles = 1,
                        TotalSizeBytes = new FileInfo(file).Length
                    };

                    // Create submission file record
                    var submissionFile = new SubmissionFile
                    {
                        Id = Guid.NewGuid(),
                        SubmissionId = submission.Id,
                        FileName = fileName,
                        FilePath = MakeRelativePath(_storagePath, file),
                        FileSizeBytes = new FileInfo(file).Length,
                        FileHash = hash,
                        FileType = "docx",
                        UploadedAt = DateTime.UtcNow,
                        Submission = submission
                    };

                    submissionFiles.Add(submissionFile);
                    result.ProcessedFiles++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file: {File}", file);
                    result.ErrorFiles++;
                }
            }

            // Step 5: Save to database
            if (submissionFiles.Any())
            {
                await _context.SubmissionFiles.AddRangeAsync(submissionFiles, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Saved {Count} submission files to database", submissionFiles.Count);
            }

            result.Message = $"Processing completed. Processed: {result.ProcessedFiles}, Duplicates: {result.DuplicateFiles}, Errors: {result.ErrorFiles}";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing nested ZIP archive");
            throw new InvalidOperationException($"Failed to process nested ZIP archive: {ex.Message}", ex);
        }
    }

    private Task ExtractArchiveAsync(string archivePath, string extractDir, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            try
            {
                ZipFile.ExtractToDirectory(archivePath, extractDir, true);
                _logger.LogInformation("Extracted archive to: {ExtractDir}", extractDir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract archive: {ArchivePath}", archivePath);
                throw;
            }
        }, cancellationToken);
    }

    private Task ExtractAllNestedSolutionZipAsync(string extractDir, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var zipFiles = Directory.GetFiles(extractDir, "solution.zip", SearchOption.AllDirectories);
            _logger.LogInformation("Found {Count} solution.zip files to extract", zipFiles.Length);

            if (zipFiles.Length == 0)
            {
                _logger.LogWarning("No solution.zip files found in extracted directory: {ExtractDir}", extractDir);
                return;
            }

            var extractedCount = 0;
            var failedCount = 0;

            foreach (var zipFile in zipFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var fileInfo = new FileInfo(zipFile);
                    if (fileInfo.Length == 0)
                    {
                        _logger.LogWarning("Skipping empty solution.zip file: {ZipFile}", zipFile);
                        failedCount++;
                        continue;
                    }

                    var zipDir = Path.Combine(Path.GetDirectoryName(zipFile)!, Path.GetFileNameWithoutExtension(zipFile) + "_extracted");
                    Directory.CreateDirectory(zipDir);

                    using (var archive = ZipFile.OpenRead(zipFile))
                    {
                        _logger.LogInformation("Validating ZIP file: {ZipFile}, Entries: {Count}", zipFile, archive.Entries.Count);
                    }

                    ZipFile.ExtractToDirectory(zipFile, zipDir, true);
                    extractedCount++;
                    _logger.LogInformation("Extracted {Index}/{Total}: {ZipFile}", extractedCount, zipFiles.Length, zipFile);
                }
                catch (InvalidDataException ex)
                {
                    _logger.LogError(ex, "Corrupted ZIP file: {ZipFile}", zipFile);
                    failedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract nested ZIP: {ZipFile}", zipFile);
                    failedCount++;
                }
            }

            _logger.LogInformation("Successfully extracted {Count}/{Total} solution.zip files. Failed: {FailedCount}",
                extractedCount, zipFiles.Length, failedCount);

            if (failedCount > 0 && extractedCount == 0)
            {
                throw new InvalidOperationException(
                    $"Failed to extract all {zipFiles.Length} solution.zip files. Check logs for details.");
            }
        }, cancellationToken);
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }

    private static (string? studentId, string? studentName) ParseStudentInfo(string fileName)
    {
        var withoutExt = Path.GetFileNameWithoutExtension(fileName);
        var parts = withoutExt.Split('_', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return (null, null);

        string? studentId = parts.Length >= 4 ? parts[3] : null;
        string? studentName = parts.Length >= 2 ? parts[^1] : null;

        return (studentId, studentName);
    }

    private static string MakeRelativePath(string root, string fullPath)
    {
        if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        return fullPath;
    }

    private static async Task<string?> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            return Convert.ToHexString(hash);
        }
        catch
        {
            return null;
        }
    }

    private static string EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }
}
