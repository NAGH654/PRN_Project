using StorageService.Entities;

namespace StorageService.Services;

public interface ISubmissionService
{
    Task<Submission?> GetByIdAsync(Guid id);
    Task<IEnumerable<Submission>> GetByStudentIdAsync(Guid studentId);
    Task<IEnumerable<Submission>> GetByExamIdAsync(Guid examId);
    Task<Submission> CreateSubmissionAsync(Guid studentId, Guid examId, Guid examSessionId);
    Task<Submission> UpdateSubmissionStatusAsync(Guid id, string status, string? notes = null);
    Task<bool> DeleteAsync(Guid id);
}
