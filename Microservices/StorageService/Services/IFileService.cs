using StorageService.Entities;

namespace StorageService.Services;

public interface IFileService
{
    Task<SubmissionFile?> GetByIdAsync(Guid id);
    Task<IEnumerable<SubmissionFile>> GetBySubmissionIdAsync(Guid submissionId);
    Task<SubmissionFile> UploadFileAsync(Guid submissionId, IFormFile file, string storagePath);
    Task<bool> DeleteFileAsync(Guid id);
    Task<string> GetFilePathAsync(Guid fileId);
}
