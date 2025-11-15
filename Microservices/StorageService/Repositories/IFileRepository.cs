using StorageService.Entities;

namespace StorageService.Repositories;

public interface IFileRepository
{
    Task<SubmissionFile?> GetByIdAsync(Guid id);
    Task<IEnumerable<SubmissionFile>> GetBySubmissionIdAsync(Guid submissionId);
    Task<SubmissionFile?> GetByHashAsync(string fileHash);
    Task<SubmissionFile> CreateAsync(SubmissionFile file);
    Task<bool> DeleteAsync(Guid id);
}
