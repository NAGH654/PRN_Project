using CoreService.Entities;

namespace CoreService.Services;

public interface IExamService
{
    Task<Exam?> GetByIdAsync(Guid id);
    Task<IEnumerable<Exam>> GetAllAsync();
    Task<IEnumerable<Exam>> GetBySubjectIdAsync(Guid subjectId);
    Task<IEnumerable<Exam>> GetBySemesterIdAsync(Guid semesterId);
    Task<Exam> CreateAsync(string title, string? description, Guid subjectId, Guid semesterId, DateTime examDate, int durationMinutes, decimal totalMarks);
    Task<Exam> UpdateAsync(Guid id, string title, string? description, Guid subjectId, Guid semesterId, DateTime examDate, int durationMinutes, decimal totalMarks);
    Task<bool> DeleteAsync(Guid id);
}
