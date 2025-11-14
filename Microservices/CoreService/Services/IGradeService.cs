using CoreService.Entities;

namespace CoreService.Services;

public interface IGradeService
{
    Task<Grade?> GetByIdAsync(Guid id);
    Task<IEnumerable<Grade>> GetByExamIdAsync(Guid examId);
    Task<IEnumerable<Grade>> GetByStudentIdAsync(Guid studentId);
    Task<Grade> CreateOrUpdateGradeAsync(Guid examId, Guid studentId, decimal score, string? feedback, Guid gradedBy);
    Task<bool> DeleteAsync(Guid id);
}
