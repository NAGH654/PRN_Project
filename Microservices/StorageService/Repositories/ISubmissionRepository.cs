using StorageService.Entities;

namespace StorageService.Repositories;

public interface ISubmissionRepository
{
    Task<Submission?> GetByIdAsync(Guid id);
    Task<IEnumerable<Submission>> GetByStudentIdAsync(Guid studentId);
    Task<IEnumerable<Submission>> GetByExamIdAsync(Guid examId);
    Task<IEnumerable<Submission>> GetBySessionIdAsync(Guid sessionId);
    Task<Submission?> GetByStudentAndExamAsync(Guid studentId, Guid examId);
    Task<IEnumerable<Submission>> GetByStatusAsync(string status);
    Task<Submission> CreateAsync(Submission submission);
    Task<Submission> UpdateAsync(Submission submission);
    Task<bool> DeleteAsync(Guid id);
}
