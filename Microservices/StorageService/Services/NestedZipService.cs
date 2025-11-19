using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using StorageService.Data;
using StorageService.Entities;
using StorageService.Models;
using Microsoft.EntityFrameworkCore;

namespace StorageService.Services;

public class NestedZipService : INestedZipService
{
    private static readonly Regex StudentIdRegex = new Regex(@"^[A-Za-z]{2}\d{6}$", RegexOptions.Compiled);
    private static readonly Regex ProhibitedPatternRegex = new Regex(@"System\.out\.println", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly StorageDbContext _context;
    private readonly ILogger<NestedZipService> _logger;
    private readonly string _storagePath;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public NestedZipService(
        StorageDbContext context,
        ILogger<NestedZipService> logger,
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
        _storagePath = configuration["Storage:BasePath"] ?? Path.Combine(AppContext.BaseDirectory, "storage");
    }

    public async Task<ProcessingResult> ProcessNestedZipArchiveAsync(UploadBatchForm form, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate exam exists and is active
            var examIdGuid = Guid.Parse(form.ExamId!);
            var exam = await ValidateExamAsync(examIdGuid, cancellationToken);
            if (exam == null)
            {
                throw new InvalidOperationException("Exam not found or not active.");
            }

            // Validate archive file
            ValidateArchive(form.Archive);

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

            var result = new ProcessingResult
            {
                JobId = jobId,
                UploadPath = uploadPath,
                ExtractPath = extractDir,
                CreatedSubmissions = new List<CreatedSubmissionInfo>()
            };

            // Step 1: Extract main archive (ZIP/RAR)
            _logger.LogInformation("Starting archive extraction...");
            await ExtractArchiveAsync(uploadPath, extractDir, cancellationToken);
            _logger.LogInformation("Archive extraction completed. Extract directory: {ExtractDir}", extractDir);

            // Step 2: Extract all nested solution.zip files
            _logger.LogInformation("Searching for nested solution.zip files...");
            await ExtractAllNestedSolutionZipAsync(extractDir, cancellationToken);

            // Step 3: Find all DOCX files from extracted directories
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

            if (docxFiles.Count == 0)
            {
                result.Message = "No DOCX files found in the archive.";
                return result;
            }

            // Step 4: Get existing file hashes for duplicate detection
            var existingFiles = await _context.SubmissionFiles
                .Where(f => f.Submission.ExamId == examIdGuid && f.FileHash != null)
                .Select(f => f.FileHash!)
                .ToListAsync(cancellationToken);
            var batchHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Step 5: Process files in batches for performance
            const int batchSize = 50;
            var processedCount = 0;

            for (int i = 0; i < docxFiles.Count; i += batchSize)
            {
                var batch = docxFiles.Skip(i).Take(batchSize).ToList();
                await ProcessBatchAsync(batch, examIdGuid, existingFiles, batchHashes, result, cancellationToken);
                processedCount += batch.Count;
                _logger.LogInformation("Processed {Processed}/{Total} files", processedCount, docxFiles.Count);
            }

            result.Message = $"Processing completed. Processed: {result.ProcessedFiles}, Duplicates: {result.DuplicateFiles}, Violations: {result.ViolationsCreated}, Images: {result.ImagesExtracted}, Errors: {result.ErrorFiles}";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing nested ZIP archive. File: {FileName}, ExamId: {ExamId}",
                form.Archive.FileName, form.ExamId);
            throw new InvalidOperationException($"Failed to process nested ZIP archive: {ex.Message}", ex);
        }
    }

    private async Task ExtractArchiveAsync(string archivePath, string extractDir, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(archivePath).ToLowerInvariant();

        if (ext == ".zip")
        {
            try
            {
                ZipFile.ExtractToDirectory(archivePath, extractDir, true);
                _logger.LogInformation("Extracted ZIP archive to: {ExtractDir}", extractDir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract ZIP file: {ArchivePath}", archivePath);
                throw new InvalidOperationException($"Failed to extract ZIP file: {ex.Message}", ex);
            }
        }
        else if (ext == ".rar")
        {
            var sevenZip = _configuration["Storage:SevenZipPath"];
            if (string.IsNullOrEmpty(sevenZip) || !File.Exists(sevenZip))
            {
                _logger.LogError("7-Zip executable not found at: {SevenZipPath}", sevenZip);
                throw new InvalidOperationException($"7-Zip executable not found. Please configure Storage:SevenZipPath in appsettings.json");
            }

            _logger.LogInformation("Extracting RAR using 7-Zip: {ArchivePath} to {ExtractDir}", archivePath, extractDir);

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = sevenZip,
                Arguments = $"x -y -o\"{extractDir}\" \"{archivePath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null)
            {
                _logger.LogError("Failed to start 7-Zip process for: {ArchivePath}", archivePath);
                throw new InvalidOperationException("Failed to start 7-Zip process.");
            }

            var output = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await proc.StandardError.ReadToEndAsync(cancellationToken);
            await proc.WaitForExitAsync(cancellationToken);

            if (proc.ExitCode != 0)
            {
                _logger.LogError("7-Zip extraction failed. ExitCode: {ExitCode}, Error: {Error}, Output: {Output}",
                    proc.ExitCode, error, output);
                throw new InvalidOperationException(
                    $"Failed to extract RAR file. The file may be corrupted, password protected, or contain invalid entries.");
            }

            _logger.LogInformation("Successfully extracted RAR file: {ArchivePath}", archivePath);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported archive format: {ext}. Only .zip and .rar are supported.");
        }
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

    private async Task<dynamic?> ValidateExamAsync(Guid examId, CancellationToken cancellationToken)
    {
        try
        {
            var coreServiceUrl = _configuration["Services:CoreService"];
            var response = await _httpClient.GetAsync($"{coreServiceUrl}/api/exams/{examId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Exam {ExamId} not found in CoreService", examId);
                return null;
            }

            // For now, assume exam is valid if it exists and check status
            // In a full implementation, we'd parse the response to check if it's active
            return new { Id = examId, IsActive = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating exam {ExamId}", examId);
            return null;
        }
    }

    private async Task ProcessBatchAsync(
        List<string> files,
        Guid examId,
        List<string> existingHashes,
        HashSet<string> batchHashes,
        ProcessingResult result,
        CancellationToken cancellationToken)
    {
        var submissions = new List<Submission>();
        var submissionFiles = new List<SubmissionFile>();
        var violations = new List<Violation>();
        var submissionImages = new List<SubmissionImage>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var fileName = Path.GetFileName(file);
                var (studentId, studentName) = ParseStudentInfo(fileName);
                var hasValidName = studentId != null && StudentIdRegex.IsMatch(studentId);

                var hash = await ComputeSha256Async(file, cancellationToken);
                var isDuplicate = (hash != null && (existingHashes.Contains(hash) || !batchHashes.Add(hash)));

                // Create submission record
                var submission = new Submission
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId ?? "UNKNOWN",
                    ExamId = examId,
                    Status = "Processing",
                    SubmittedAt = DateTime.UtcNow,
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

                // Check for naming violation
                if (!hasValidName)
                {
                    violations.Add(new Violation
                    {
                        Id = Guid.NewGuid(),
                        SubmissionId = submission.Id,
                        Type = "Naming",
                        Severity = "Warning",
                        Description = $"Invalid naming convention for file: {fileName}. Expected format: StudentID_ExamName.ext",
                        DetectedAt = DateTime.UtcNow
                    });
                    result.ViolationsCreated++;
                }

                // Check for duplicate violation
                if (isDuplicate)
                {
                    violations.Add(new Violation
                    {
                        Id = Guid.NewGuid(),
                        SubmissionId = submission.Id,
                        Type = "Duplicate",
                        Severity = "Error",
                        Description = $"Duplicate content detected: {fileName}",
                        DetectedAt = DateTime.UtcNow
                    });
                    result.ViolationsCreated++;
                    result.DuplicateFiles++;
                    continue; // Skip duplicate files
                }

                // Check for content violations in code files
                if (IsCodeFile(file))
                {
                    try
                    {
                        var text = await File.ReadAllTextAsync(file, cancellationToken);
                        if (ProhibitedPatternRegex.IsMatch(text))
                        {
                            violations.Add(new Violation
                            {
                                Id = Guid.NewGuid(),
                                SubmissionId = submission.Id,
                                Type = "Content",
                                Severity = "Warning",
                                Description = $"Prohibited pattern 'System.out.println' found in: {fileName}",
                                DetectedAt = DateTime.UtcNow
                            });
                            result.ViolationsCreated++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read file for pattern scan: {File}", file);
                    }
                }

                // Extract images from DOCX
                if (string.Equals(Path.GetExtension(file), ".docx", StringComparison.OrdinalIgnoreCase))
                {
                    var imagesExtracted = await ExtractImagesFromDocxAsync(file, _storagePath, submission, cancellationToken);
                    result.ImagesExtracted += imagesExtracted;
                }

                submissions.Add(submission);
                submissionFiles.Add(submissionFile);
                result.ProcessedFiles++;

                // Add to created submissions list
                result.CreatedSubmissions.Add(new CreatedSubmissionInfo
                {
                    SubmissionId = submission.Id,
                    StudentId = submission.StudentId,
                    StudentName = studentName ?? "Unknown",
                    FileName = submissionFile.FileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process file: {File}. Continuing with other files.", file);
                result.ErrorFiles++;
            }
        }

        // Save batch to database
        if (submissions.Any())
        {
            await _context.Submissions.AddRangeAsync(submissions, cancellationToken);
            await _context.SubmissionFiles.AddRangeAsync(submissionFiles, cancellationToken);

            if (violations.Any())
            {
                await _context.Violations.AddRangeAsync(violations, cancellationToken);
            }

            if (submissionImages.Any())
            {
                await _context.SubmissionImages.AddRangeAsync(submissionImages, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Update submission status to Pending
            foreach (var submission in submissions)
            {
                submission.Status = "Pending";
                submission.ProcessedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool IsCodeFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".cs" or ".java" or ".cpp" or ".c" or ".py" or ".js" or ".ts";
    }

    private static void ValidateArchive(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".zip" && ext != ".rar")
        {
            throw new InvalidOperationException("Invalid file format. Only .zip and .rar are supported.");
        }
        // 2 GB limit for large exam archives
        if (file.Length > 2L * 1024L * 1024L * 1024L)
        {
            throw new InvalidOperationException("File is too large. Maximum 2GB.");
        }
    }

    private async Task<int> ExtractImagesFromDocxAsync(string docxPath, string storageRoot, Submission submission, CancellationToken cancellationToken)
    {
        try
        {
            using var archive = ZipFile.OpenRead(docxPath);
            var mediaEntries = archive.Entries
                .Where(e => e.FullName.StartsWith("word/media/", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(e.Name))
                .ToList();

            if (mediaEntries.Count == 0) return 0;

            var imagesDir = EnsureDirectory(Path.Combine(storageRoot, "jobs", submission.Id.ToString(), "images"));
            var images = new List<SubmissionImage>();

            foreach (var entry in mediaEntries)
            {
                var outPath = Path.Combine(imagesDir, entry.Name);
                entry.ExtractToFile(outPath, true);

                images.Add(new SubmissionImage
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submission.Id,
                    ImageName = entry.Name,
                    ImagePath = MakeRelativePath(storageRoot, outPath),
                    ImageSizeBytes = new FileInfo(outPath).Length,
                    ExtractedAt = DateTime.UtcNow,
                    Submission = submission
                });
            }

            if (images.Any())
            {
                await _context.SubmissionImages.AddRangeAsync(images, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return images.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract images from DOCX: {File}", submission.Id);
            return 0;
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
