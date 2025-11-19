using System.Security.Cryptography;
using StorageService.Entities;
using StorageService.Repositories;

namespace StorageService.Services;

public class FileService : IFileService
{
    private readonly IFileRepository _fileRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly ILogger<FileService> _logger;

    public FileService(
        IFileRepository fileRepository,
        ISubmissionRepository submissionRepository,
        ILogger<FileService> logger)
    {
        _fileRepository = fileRepository;
        _submissionRepository = submissionRepository;
        _logger = logger;
    }

    public async Task<SubmissionFile?> GetByIdAsync(Guid id)
    {
        return await _fileRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<SubmissionFile>> GetBySubmissionIdAsync(Guid submissionId)
    {
        return await _fileRepository.GetBySubmissionIdAsync(submissionId);
    }

    public async Task<SubmissionFile> UploadFileAsync(Guid submissionId, IFormFile file, string storagePath)
    {
        // Validate submission exists
        var submission = await _submissionRepository.GetByIdAsync(submissionId);
        if (submission == null)
        {
            _logger.LogWarning("Submission {SubmissionId} not found", submissionId);
            throw new KeyNotFoundException($"Submission with ID {submissionId} not found");
        }

        // Validate file
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Invalid file upload attempted");
            throw new ArgumentException("File is required and cannot be empty", nameof(file));
        }

        // Validate file size (max 50MB)
        const long maxFileSize = 50 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            _logger.LogWarning("File too large: {Size} bytes", file.Length);
            throw new ArgumentException($"File size cannot exceed {maxFileSize / (1024 * 1024)}MB", nameof(file));
        }

        // Create storage directory if it doesn't exist
        var submissionFolder = Path.Combine(storagePath, submissionId.ToString());
        Directory.CreateDirectory(submissionFolder);

        // Generate unique filename
        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(submissionFolder, uniqueFileName);

        // Calculate file hash
        string fileHash;
        using (var stream = file.OpenReadStream())
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream);
            fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        // Check for duplicate file
        var existingFile = await _fileRepository.GetByHashAsync(fileHash);
        if (existingFile != null)
        {
            _logger.LogWarning("Duplicate file detected: {Hash}", fileHash);
            throw new InvalidOperationException("This file has already been uploaded");
        }

        // Save file to disk
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        // Determine if file is an image
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff" };
        var isImage = imageExtensions.Contains(fileExtension.ToLowerInvariant());

        // Create file record
        var submissionFile = new SubmissionFile
        {
            SubmissionId = submissionId,
            FileName = file.FileName,
            FilePath = filePath,
            FileType = file.ContentType,
            FileSizeBytes = file.Length,
            FileHash = fileHash,
            IsImage = isImage
        };

        var created = await _fileRepository.CreateAsync(submissionFile);

        // Update submission totals
        submission.TotalFiles++;
        submission.TotalSizeBytes += file.Length;
        await _submissionRepository.UpdateAsync(submission);

        _logger.LogInformation("File {FileName} uploaded successfully for submission {SubmissionId}", file.FileName, submissionId);
        
        return created;
    }

    public async Task<bool> DeleteFileAsync(Guid id)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null)
        {
            _logger.LogWarning("File {Id} not found for deletion", id);
            return false;
        }

        // Delete physical file
        if (File.Exists(file.FilePath))
        {
            try
            {
                File.Delete(file.FilePath);
                _logger.LogInformation("Physical file deleted: {Path}", file.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete physical file: {Path}", file.FilePath);
            }
        }

        // Update submission totals
        var submission = await _submissionRepository.GetByIdAsync(file.SubmissionId);
        if (submission != null)
        {
            submission.TotalFiles--;
            submission.TotalSizeBytes -= file.FileSizeBytes;
            await _submissionRepository.UpdateAsync(submission);
        }

        var deleted = await _fileRepository.DeleteAsync(id);
        if (deleted)
        {
            _logger.LogInformation("File {Id} deleted successfully", id);
        }
        
        return deleted;
    }

    public async Task<string> GetFilePathAsync(Guid fileId)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null)
        {
            _logger.LogWarning("File {Id} not found", fileId);
            throw new KeyNotFoundException($"File with ID {fileId} not found");
        }

        if (!File.Exists(file.FilePath))
        {
            _logger.LogError("Physical file not found: {Path}", file.FilePath);
            throw new FileNotFoundException("Physical file not found", file.FilePath);
        }

        return file.FilePath;
    }
}
