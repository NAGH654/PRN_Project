using CoreService.Entities;

namespace CoreService.Repositories;

public interface IExamSessionRepository
{
    Task<ExamSession?> GetByIdAsync(Guid id);
    Task<IEnumerable<ExamSession>> GetAllAsync();
    Task<IEnumerable<ExamSession>> GetActiveAsync();
    Task<IEnumerable<ExamSession>> GetByExamIdAsync(Guid examId);
    Task<ExamSession> CreateAsync(ExamSession session);
    Task<ExamSession> UpdateAsync(ExamSession session);
    Task DeleteAsync(Guid id);
}
