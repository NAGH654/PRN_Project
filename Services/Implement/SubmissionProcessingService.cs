using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Repositories.Data;
using Repositories.Entities;
using Repositories.Entities.Enums;
using Services.Dtos;
using Services.Dtos.Requests;
using Services.Dtos.Responses;
using Services.Interfaces;
using Services.Models;

namespace Services.Implement
{
    public class SubmissionProcessingService : ISubmissionProcessingService
    {
        private static readonly Regex StudentIdRegex = new("^[A-Za-z]{2}\\d{6}$", RegexOptions.Compiled);

        private readonly AppDbContext _db;
        private readonly StorageOptions _storageOptions;
        private readonly ILogger<SubmissionProcessingService> _logger;
        private readonly INotificationService _notificationService;

        public SubmissionProcessingService(
            AppDbContext db,
            IOptions<StorageOptions> storageOptions,
            ILogger<SubmissionProcessingService> logger,
            INotificationService notificationService)
        {
            _db = db;
            _storageOptions = storageOptions.Value;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<ProcessingResult> ProcessArchiveAsync(UploadBatchForm form, CancellationToken cancellationToken = default)
        {
            var session = await _db.ExamSessions.FirstOrDefaultAsync(x => x.SessionId == form.SessionId, cancellationToken);
            if (session == null)
            {
                throw new InvalidOperationException("Exam session not found.");
            }

            ValidateArchive(form.Archive);

            var jobId = Guid.NewGuid().ToString();
            var root = EnsureDirectory(Path.Combine(AppContext.BaseDirectory, _storageOptions.Root));
            var uploadsDir = EnsureDirectory(Path.Combine(root, "uploads", DateTime.UtcNow.ToString("yyyyMMddHHmmss")));
            var jobsDir = EnsureDirectory(Path.Combine(root, "jobs", jobId));
            var extractDir = EnsureDirectory(Path.Combine(jobsDir, "extract"));

            var uploadPath = Path.Combine(uploadsDir, SanitizeFileName(form.Archive.FileName));
            await using (var fs = File.Create(uploadPath))
            {
                await form.Archive.CopyToAsync(fs, cancellationToken);
            }

            await ExtractArchiveAsync(uploadPath, extractDir, cancellationToken);

            var result = new ProcessingResult
            {
                JobId = jobId,
                UploadPath = uploadPath,
                ExtractPath = extractDir
            };

            var files = Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories);
            result.TotalFiles = files.Length;

            var existingHashes = await _db.Submissions
                .Where(s => s.SessionId == session.SessionId && s.ContentHash != null)
                .Select(s => s.ContentHash!)
                .ToListAsync(cancellationToken);
            var batchHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sessionSubmissionCount = await _db.Submissions
                .CountAsync(s => s.SessionId == session.SessionId, cancellationToken);

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = Path.GetFileName(file);
                var (studentId, studentName) = ParseStudentInfo(fileName);
                var hasValidName = studentId != null && StudentIdRegex.IsMatch(studentId);

                var hash = await ComputeSha256Async(file, cancellationToken);
                var isDuplicate = (hash != null && (existingHashes.Contains(hash) || !batchHashes.Add(hash)));

                var submission = new Submission
                {
                    SessionId = session.SessionId,
                    StudentId = studentId ?? "UNKNOWN",
                    StudentName = studentName,
                    FileName = fileName,
                    FilePath = MakeRelativePath(root, file),
                    FileSize = new FileInfo(file).Length,
                    ContentHash = hash,
                    SubmissionTime = DateTime.UtcNow,
                    Status = SubmissionStatus.Processing
                };

                _db.Submissions.Add(submission);
                await _db.SaveChangesAsync(cancellationToken);
                result.SubmissionsCreated++;

                var violationsForSubmission = new List<Violation>();

                if (!hasValidName)
                {
                    var violation = new Violation
                    {
                        SubmissionId = submission.SubmissionId,
                        ViolationType = ViolationType.Naming,
                        Severity = ViolationSeverity.Warning,
                        Description = "Invalid naming convention. Expected StudentID_ExamName.ext",
                        DetectedAt = DateTime.UtcNow
                    };
                    _db.Violations.Add(violation);
                    violationsForSubmission.Add(violation);
                    result.ViolationsCreated++;
                }
                if (isDuplicate)
                {
                    var violation = new Violation
                    {
                        SubmissionId = submission.SubmissionId,
                        ViolationType = ViolationType.Duplicate,
                        Severity = ViolationSeverity.Error,
                        Description = "Duplicate content detected within session.",
                        DetectedAt = DateTime.UtcNow
                    };
                    _db.Violations.Add(violation);
                    violationsForSubmission.Add(violation);
                    result.ViolationsCreated++;
                }

                // Simple prohibited pattern scan for code files
                if (IsCodeFile(file))
                {
                    var text = await File.ReadAllTextAsync(file, cancellationToken);
                    if (text.Contains("System.out.println", StringComparison.Ordinal))
                    {
                        var violation = new Violation
                        {
                            SubmissionId = submission.SubmissionId,
                            ViolationType = ViolationType.Content,
                            Severity = ViolationSeverity.Warning,
                            Description = "Prohibited pattern 'System.out.println' found.",
                            DetectedAt = DateTime.UtcNow
                        };
                        _db.Violations.Add(violation);
                        violationsForSubmission.Add(violation);
                        result.ViolationsCreated++;
                    }
                }

                // Extract images from DOCX
                if (string.Equals(Path.GetExtension(file), ".docx", StringComparison.OrdinalIgnoreCase))
                {
                    result.ImagesExtracted += await ExtractImagesFromDocxAsync(file, root, submission, cancellationToken);
                }

                submission.Status = SubmissionStatus.Pending;
                _db.Submissions.Update(submission);
                await _db.SaveChangesAsync(cancellationToken);

                sessionSubmissionCount = await DispatchSubmissionNotificationsAsync(
                    session,
                    new[] { submission },
                    violationsForSubmission,
                    sessionSubmissionCount);

                // add created submission info for immediate UI usage
                result.CreatedSubmissions.Add(new CreatedSubmissionInfo
                {
                    SubmissionId = submission.SubmissionId,
                    StudentId = submission.StudentId,
                    StudentName = submission.StudentName,
                    FileName = submission.FileName
                });
            }

            return result;
        }

        public async Task<ProcessingResult> ProcessNestedZipArchiveAsync(UploadBatchForm form, CancellationToken cancellationToken = default)
        {
            try
            {
                var session = await _db.ExamSessions.FirstOrDefaultAsync(x => x.SessionId == form.SessionId, cancellationToken);
                if (session == null)
                {
                    throw new InvalidOperationException("Exam session not found.");
                }

                ValidateArchive(form.Archive);

                var jobId = Guid.NewGuid().ToString();
                var root = EnsureDirectory(Path.Combine(AppContext.BaseDirectory, _storageOptions.Root));
                var uploadsDir = EnsureDirectory(Path.Combine(root, "uploads", DateTime.UtcNow.ToString("yyyyMMddHHmmss")));
                var jobsDir = EnsureDirectory(Path.Combine(root, "jobs", jobId));
                var extractDir = EnsureDirectory(Path.Combine(jobsDir, "extract"));

                var uploadPath = Path.Combine(uploadsDir, SanitizeFileName(form.Archive.FileName));
                _logger.LogInformation("Uploading file: {FileName}, Size: {Size} bytes", form.Archive.FileName, form.Archive.Length);
                
                await using (var fs = File.Create(uploadPath))
                {
                    await form.Archive.CopyToAsync(fs, cancellationToken);
                }
                _logger.LogInformation("File saved to: {UploadPath}", uploadPath);

                _logger.LogInformation("Starting RAR extraction...");
                await ExtractArchiveAsync(uploadPath, extractDir, cancellationToken);
                _logger.LogInformation("RAR extraction completed. Extract directory: {ExtractDir}", extractDir);

                var result = new ProcessingResult
                {
                    JobId = jobId,
                    UploadPath = uploadPath,
                    ExtractPath = extractDir
                };

                // Step 1: Automatically find and extract all solution.zip files from the RAR
                _logger.LogInformation("Searching for nested solution.zip files...");
                await ExtractAllNestedSolutionZipAsync(extractDir, cancellationToken);

                // Step 2: Find only DOCX files from extracted solution.zip directories
                // Ignore ZIP files, folders, images, and other files
                var docxFiles = new List<string>();
                
                // Find DOCX in extracted solution.zip directories (solution_extracted folders)
                var extractedZipDirs = Directory.GetDirectories(extractDir, "solution_extracted", SearchOption.AllDirectories);
                foreach (var zipDir in extractedZipDirs)
                {
                    docxFiles.AddRange(Directory.GetFiles(zipDir, "*.docx", SearchOption.AllDirectories));
                }
                
                // Also check for DOCX directly in extractDir (if any student put DOCX directly)
                docxFiles.AddRange(Directory.GetFiles(extractDir, "*.docx", SearchOption.AllDirectories));
                
                // Remove duplicates
                docxFiles = docxFiles.Distinct().ToList();
                
                result.TotalFiles = docxFiles.Count;
                _logger.LogInformation("Found {Count} DOCX files to process", docxFiles.Count);

                var existingHashes = await _db.Submissions
                    .Where(s => s.SessionId == session.SessionId && s.ContentHash != null)
                    .Select(s => s.ContentHash!)
                    .ToListAsync(cancellationToken);
                var batchHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var sessionSubmissionCount = await _db.Submissions
                    .CountAsync(s => s.SessionId == session.SessionId, cancellationToken);

                const int batchSize = 50; // Process in batches to improve performance
                var submissions = new List<Submission>();
                var violationsMap = new Dictionary<int, List<Violation>>(); // Index in submissions -> violations
                var processedCount = 0;

                foreach (var file in docxFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var fileName = Path.GetFileName(file);
                        var (studentId, studentName) = ParseStudentInfo(fileName);
                        var hasValidName = studentId != null && StudentIdRegex.IsMatch(studentId);

                        var hash = await ComputeSha256Async(file, cancellationToken);
                        var isDuplicate = (hash != null && (existingHashes.Contains(hash) || !batchHashes.Add(hash)));

                        var submission = new Submission
                        {
                            SessionId = session.SessionId,
                            StudentId = studentId ?? "UNKNOWN",
                            StudentName = studentName,
                            FileName = fileName,
                            FilePath = MakeRelativePath(root, file),
                            FileSize = new FileInfo(file).Length,
                            ContentHash = hash,
                            SubmissionTime = DateTime.UtcNow,
                            Status = SubmissionStatus.Processing
                        };

                        var submissionIndex = submissions.Count;
                        submissions.Add(submission);

                        var submissionViolations = new List<Violation>();

                        if (!hasValidName)
                        {
                            submissionViolations.Add(new Violation
                            {
                                SubmissionId = Guid.Empty, // Will be set after save
                                ViolationType = ViolationType.Naming,
                                Severity = ViolationSeverity.Warning,
                                Description = $"Invalid naming convention for file: {fileName}",
                                DetectedAt = DateTime.UtcNow
                            });
                        }
                        if (isDuplicate)
                        {
                            submissionViolations.Add(new Violation
                            {
                                SubmissionId = Guid.Empty, // Will be set after save
                                ViolationType = ViolationType.Duplicate,
                                Severity = ViolationSeverity.Error,
                                Description = $"Duplicate content detected: {fileName}",
                                DetectedAt = DateTime.UtcNow
                            });
                        }

                        // Simple prohibited pattern scan for code files
                        if (IsCodeFile(file))
                        {
                            try
                            {
                                var text = await File.ReadAllTextAsync(file, cancellationToken);
                                if (text.Contains("System.out.println", StringComparison.Ordinal))
                                {
                                    submissionViolations.Add(new Violation
                                    {
                                        SubmissionId = Guid.Empty, // Will be set after save
                                        ViolationType = ViolationType.Content,
                                        Severity = ViolationSeverity.Warning,
                                        Description = $"Prohibited pattern 'System.out.println' found in: {fileName}",
                                        DetectedAt = DateTime.UtcNow
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to read file for pattern scan: {File}", file);
                            }
                        }

                        if (submissionViolations.Count > 0)
                        {
                            violationsMap[submissionIndex] = submissionViolations;
                        }

                        // Save batch when reaching batch size
                        if (submissions.Count >= batchSize)
                        {
                            await SaveBatchAsync(submissions, violationsMap, cancellationToken);
                            // collect created submission info
                            foreach (var s in submissions)
                            {
                                result.CreatedSubmissions.Add(new CreatedSubmissionInfo
                                {
                                    SubmissionId = s.SubmissionId,
                                    StudentId = s.StudentId,
                                    StudentName = s.StudentName,
                                    FileName = s.FileName
                                });
                            }
                            result.SubmissionsCreated += submissions.Count;
                            var violationsForBatch = violationsMap.Values.SelectMany(v => v).ToList();
                            result.ViolationsCreated += violationsForBatch.Count;
                            violationsMap.Clear();

                            // Extract images for this batch
                            foreach (var sub in submissions.Where(s => string.Equals(Path.GetExtension(s.FileName), ".docx", StringComparison.OrdinalIgnoreCase)))
                            {
                                try
                                {
                                    var docxPath = Path.Combine(root, sub.FilePath);
                                    if (File.Exists(docxPath))
                                    {
                                        result.ImagesExtracted += await ExtractImagesFromDocxAsync(docxPath, root, sub, cancellationToken);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to extract images from DOCX: {File}", sub.FileName);
                                }
                            }

                            // Update status to Pending
                            foreach (var sub in submissions)
                            {
                                sub.Status = SubmissionStatus.Pending;
                            }
                            await _db.SaveChangesAsync(cancellationToken);

                            sessionSubmissionCount = await DispatchSubmissionNotificationsAsync(
                                session,
                                submissions,
                                violationsForBatch,
                                sessionSubmissionCount);

                            processedCount += submissions.Count;
                            _logger.LogInformation("Processed {Processed}/{Total} files", processedCount, docxFiles.Count);

                            submissions.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to process file: {File}. Continuing with other files.", file);
                        // Continue with other files instead of stopping
                    }
                }

                // Save remaining items
                if (submissions.Count > 0)
                {
                    await SaveBatchAsync(submissions, violationsMap, cancellationToken);
                    foreach (var s in submissions)
                    {
                        result.CreatedSubmissions.Add(new CreatedSubmissionInfo
                        {
                            SubmissionId = s.SubmissionId,
                            StudentId = s.StudentId,
                            StudentName = s.StudentName,
                            FileName = s.FileName
                        });
                    }
                    result.SubmissionsCreated += submissions.Count;
                    var finalBatchViolations = violationsMap.Values.SelectMany(v => v).ToList();
                    result.ViolationsCreated += finalBatchViolations.Count;
                    violationsMap.Clear();

                    // Extract images for remaining batch
                    foreach (var sub in submissions.Where(s => string.Equals(Path.GetExtension(s.FileName), ".docx", StringComparison.OrdinalIgnoreCase)))
                    {
                        try
                        {
                            var docxPath = Path.Combine(root, sub.FilePath);
                            if (File.Exists(docxPath))
                            {
                                result.ImagesExtracted += await ExtractImagesFromDocxAsync(docxPath, root, sub, cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to extract images from DOCX: {File}", sub.FileName);
                        }
                    }

                    // Update status to Pending
                    foreach (var sub in submissions)
                    {
                        sub.Status = SubmissionStatus.Pending;
                    }
                    await _db.SaveChangesAsync(cancellationToken);

                    sessionSubmissionCount = await DispatchSubmissionNotificationsAsync(
                        session,
                        submissions,
                        finalBatchViolations,
                        sessionSubmissionCount);

                    processedCount += submissions.Count;
                    _logger.LogInformation("Processed final batch: {Processed}/{Total} files", processedCount, docxFiles.Count);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing nested ZIP archive. File: {FileName}, SessionId: {SessionId}", 
                    form.Archive.FileName, form.SessionId);
                throw new InvalidOperationException($"Failed to process nested ZIP archive: {ex.Message}", ex);
            }
        }

        private async Task<int> DispatchSubmissionNotificationsAsync(
            ExamSession session,
            IEnumerable<Submission> submissions,
            IEnumerable<Violation> violations,
            int currentSubmissionCount)
        {
            if (_notificationService == null)
            {
                return currentSubmissionCount;
            }

            var total = currentSubmissionCount;
            var violationLookup = violations
                .GroupBy(v => v.SubmissionId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var submission in submissions)
            {
                total++;
                var uploadNotification = new SubmissionUploadedNotificationDto
                {
                    SubmissionId = submission.SubmissionId,
                    SessionId = submission.SessionId,
                    StudentId = submission.StudentId,
                    TotalSubmissions = total
                };
                await _notificationService.NotifySubmissionUploadedAsync(uploadNotification, session.ExamId);

                if (violationLookup.TryGetValue(submission.SubmissionId, out var submissionViolations))
                {
                    foreach (var violation in submissionViolations)
                    {
                        var violationNotification = new ViolationDetectedNotificationDto
                        {
                            SubmissionId = submission.SubmissionId,
                            ViolationId = violation.ViolationId,
                            ViolationType = violation.ViolationType.ToString(),
                            Severity = violation.Severity.ToString(),
                            Description = violation.Description,
                            StudentId = submission.StudentId
                        };
                        await _notificationService.NotifyViolationDetectedAsync(violationNotification, session.ExamId);
                    }
                }
            }

            return total;
        }

        private Task ExtractAllNestedSolutionZipAsync(string extractDir, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                // Find all solution.zip files recursively in the extracted RAR
                var zipFiles = Directory.GetFiles(extractDir, "solution.zip", SearchOption.AllDirectories);
                
                _logger.LogInformation("Found {Count} solution.zip files to extract", zipFiles.Length);
                
                if (zipFiles.Length == 0)
                {
                    _logger.LogWarning("No solution.zip files found in extracted RAR directory: {ExtractDir}", extractDir);
                    return;
                }
                
                var extractedCount = 0;
                var failedCount = 0;
                
                foreach (var zipFile in zipFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    try
                    {
                        // Check if ZIP file is valid before extracting
                        var fileInfo = new FileInfo(zipFile);
                        if (fileInfo.Length == 0)
                        {
                            _logger.LogWarning("Skipping empty solution.zip file: {ZipFile}", zipFile);
                            failedCount++;
                            continue;
                        }
                        
                        var zipDir = Path.Combine(Path.GetDirectoryName(zipFile)!, Path.GetFileNameWithoutExtension(zipFile) + "_extracted");
                        Directory.CreateDirectory(zipDir);
                        
                        // Validate ZIP file before extracting
                        using (var archive = ZipFile.OpenRead(zipFile))
                        {
                            var entryCount = archive.Entries.Count;
                            _logger.LogInformation("Validating ZIP file: {ZipFile}, Entries: {Count}", zipFile, entryCount);
                        }
                        
                        // Extract after validation
                        ZipFile.ExtractToDirectory(zipFile, zipDir, true);
                        extractedCount++;
                        
                        _logger.LogInformation("Extracted {Index}/{Total}: {ZipFile}", extractedCount, zipFiles.Length, zipFile);
                    }
                    catch (System.IO.InvalidDataException ex)
                    {
                        // ZIP file is corrupted or incomplete
                        _logger.LogError(ex, "Corrupted or incomplete ZIP file: {ZipFile}. Error: {Message}", zipFile, ex.Message);
                        failedCount++;
                        // Continue with other files
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract nested ZIP: {ZipFile}. Error: {Message}", zipFile, ex.Message);
                        failedCount++;
                        // Continue with other files
                    }
                }
                
                _logger.LogInformation("Successfully extracted {Count}/{Total} solution.zip files. Failed: {FailedCount}", 
                    extractedCount, zipFiles.Length, failedCount);
                
                if (failedCount > 0 && extractedCount == 0)
                {
                    throw new InvalidOperationException(
                        $"Failed to extract all {zipFiles.Length} solution.zip files. " +
                        $"This may indicate corrupted ZIP files or issues with the archive structure. " +
                        $"Please check the log for details.");
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

            // Expected: ... _ <StudentId> _ ... _ <StudentName>
            // Example: SWD392_PE_SU25_SE1823347_KieuVietAnh
            string? studentId = parts.Length >= 4 ? parts[3] : null;
            string? studentName = parts.Length >= 2 ? parts[^1] : null;

            return (studentId, studentName);
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
                throw new InvalidOperationException("Invalid file format. Only .zip or .rar allowed.");
            }
            // 500 MB
            if (file.Length > 500L * 1024L * 1024L)
            {
                throw new InvalidOperationException("File is too large. Maximum 500MB.");
            }
        }

        private async Task ExtractArchiveAsync(string archivePath, string destinationDir, CancellationToken cancellationToken)
        {
            var ext = Path.GetExtension(archivePath).ToLowerInvariant();
            if (ext == ".zip")
            {
                try
                {
                    ZipFile.ExtractToDirectory(archivePath, destinationDir, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract ZIP file: {ArchivePath}", archivePath);
                    throw new InvalidOperationException($"Failed to extract ZIP file: {ex.Message}", ex);
                }
            }
            else if (ext == ".rar")
            {
                var sevenZip = _storageOptions.SevenZipPath;
                if (!File.Exists(sevenZip))
                {
                    _logger.LogError("7-Zip executable not found at: {SevenZipPath}", sevenZip);
                    throw new InvalidOperationException($"7-Zip executable not found at: {sevenZip}. Please configure StorageOptions.SevenZipPath in appsettings.json");
                }
                
                _logger.LogInformation("Extracting RAR using 7-Zip: {ArchivePath} to {DestinationDir}", archivePath, destinationDir);
                
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = sevenZip,
                    Arguments = $"x -y -o\"{destinationDir}\" \"{archivePath}\"",
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
                        $"Failed to extract RAR file. ExitCode: {proc.ExitCode}. " +
                        $"The file may be corrupted, password protected, or contain invalid entries. " +
                        $"Error details: {error}");
                }
                
                _logger.LogInformation("Successfully extracted RAR file: {ArchivePath}", archivePath);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported archive format: {ext}. Only .zip and .rar are supported.");
            }
        }

        private static async Task<string?> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
        {
            await using var stream = File.OpenRead(filePath);
            using var sha = SHA256.Create();
            var hash = await sha.ComputeHashAsync(stream, cancellationToken);
            return Convert.ToHexString(hash);
        }

        private static string EnsureDirectory(string path)
        {
            Directory.CreateDirectory(path);
            return path;
        }

        private async Task SaveBatchAsync(List<Submission> submissions, Dictionary<int, List<Violation>> violationsMap, CancellationToken cancellationToken)
        {
            if (submissions.Count == 0) return;

            try
            {
                await _db.Submissions.AddRangeAsync(submissions, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                // Now update violation SubmissionIds after submissions are saved with GUIDs
                // Match by index: violationsMap[key] corresponds to submissions[key]
                var allViolations = new List<Violation>();
                foreach (var kvp in violationsMap)
                {
                    var submissionIndex = kvp.Key;
                    if (submissionIndex < submissions.Count)
                    {
                        var submission = submissions[submissionIndex];
                        foreach (var violation in kvp.Value)
                        {
                            violation.SubmissionId = submission.SubmissionId;
                            allViolations.Add(violation);
                        }
                    }
                }

                if (allViolations.Count > 0)
                {
                    await _db.Violations.AddRangeAsync(allViolations, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save batch of {Count} submissions", submissions.Count);
                throw;
            }
        }

        private static string MakeRelativePath(string root, string fullPath)
        {
            var rootFix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullFix = Path.GetFullPath(fullPath);
            if (fullFix.StartsWith(rootFix, StringComparison.OrdinalIgnoreCase))
            {
                return fullFix[rootFix.Length..];
            }
            return fullFix;
        }

        private async Task<int> ExtractImagesFromDocxAsync(string docxPath, string storageRoot, Submission submission, CancellationToken cancellationToken)
        {
            var count = 0;
            using var archive = ZipFile.OpenRead(docxPath);
            var mediaEntries = archive.Entries.Where(e => e.FullName.StartsWith("word/media/", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(e.Name)).ToList();
            if (mediaEntries.Count == 0) return 0;

            var imagesDir = EnsureDirectory(Path.Combine(storageRoot, "jobs", submission.SubmissionId.ToString(), "images"));

            foreach (var entry in mediaEntries)
            {
                var outPath = Path.Combine(imagesDir, entry.Name);
                entry.ExtractToFile(outPath, true);
                var img = new SubmissionImage
                {
                    SubmissionId = submission.SubmissionId,
                    ImageName = entry.Name,
                    ImagePath = MakeRelativePath(storageRoot, outPath),
                    ImageSize = new FileInfo(outPath).Length,
                    ExtractedAt = DateTime.UtcNow
                };
                _db.SubmissionImages.Add(img);
                count++;
            }
            await _db.SaveChangesAsync(cancellationToken);
            return count;
        }
    }
}


