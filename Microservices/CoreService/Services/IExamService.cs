using CoreService.DTOs;
using CoreService.Entities;

namespace CoreService.Services;

public interface IExamService
{
    Task<ExamDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ExamDto>> GetAllAsync();
    Task<IEnumerable<ExamDto>> GetBySubjectIdAsync(Guid subjectId);
    Task<IEnumerable<ExamDto>> GetBySemesterIdAsync(Guid semesterId);
    Task<Exam> CreateAsync(string title, string? description, Guid subjectId, Guid semesterId, DateTime examDate, int durationMinutes, decimal totalMarks);
    Task<Exam> UpdateAsync(Guid id, string title, string? description, Guid subjectId, Guid semesterId, DateTime examDate, int durationMinutes, decimal totalMarks);
    Task<bool> DeleteAsync(Guid id);

    // Rubric Management
    Task<RubricItem> AddRubricItemAsync(Guid examId, string criteria, string? description, decimal maxPoints);
    Task<bool> RemoveRubricItemAsync(Guid examId, Guid rubricItemId);

    // Publishing
    Task<Exam> PublishExamAsync(Guid examId);

    // Examiner Assignment
    Task<ExaminerAssignment> AssignExaminerAsync(Guid examSessionId, Guid examinerId, string role = "Examiner");
    Task<bool> RemoveExaminerAssignmentAsync(Guid examSessionId, Guid examinerId);
}
