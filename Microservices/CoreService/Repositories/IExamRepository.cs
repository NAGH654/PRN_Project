using CoreService.Entities;

namespace CoreService.Repositories;

public interface IExamRepository
{
    Task<Exam?> GetByIdAsync(Guid id);
    Task<IEnumerable<Exam>> GetAllAsync();
    Task<IEnumerable<Exam>> GetBySubjectIdAsync(Guid subjectId);
    Task<IEnumerable<Exam>> GetBySemesterIdAsync(Guid semesterId);
    Task<Exam> CreateAsync(Exam exam);
    Task<Exam> UpdateAsync(Exam exam);
    Task<bool> DeleteAsync(Guid id);
}
