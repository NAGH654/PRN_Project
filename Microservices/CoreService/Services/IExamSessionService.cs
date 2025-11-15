using CoreService.Entities;

namespace CoreService.Services;

public interface IExamSessionService
{
    Task<ExamSession?> GetByIdAsync(Guid id);
    Task<IEnumerable<ExamSession>> GetAllAsync();
    Task<IEnumerable<ExamSession>> GetActiveAsync();
    Task<IEnumerable<ExamSession>> GetByExamIdAsync(Guid examId);
    Task<ExamSession> CreateAsync(ExamSession session);
    Task<ExamSession> UpdateAsync(Guid id, ExamSession session);
    Task DeleteAsync(Guid id);
}
