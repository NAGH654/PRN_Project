using CoreService.Entities;

namespace CoreService.Repositories;

public interface IGradeRepository
{
    Task<Grade?> GetByIdAsync(Guid id);
    Task<IEnumerable<Grade>> GetByExamIdAsync(Guid examId);
    Task<IEnumerable<Grade>> GetByStudentIdAsync(Guid studentId);
    Task<Grade?> GetByExamAndStudentAsync(Guid examId, Guid studentId);
    Task<Grade> CreateAsync(Grade grade);
    Task<Grade> UpdateAsync(Grade grade);
    Task<bool> DeleteAsync(Guid id);
}
