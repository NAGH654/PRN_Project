using CoreService.DTOs;
using CoreService.Entities;

namespace CoreService.Services;

public interface IExamService
{
    Task<ExamDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ExamDto>> GetAllAsync();
    Task<IEnumerable<ExamDto>> GetBySubjectIdAsync(Guid subjectId);
    Task<IEnumerable<ExamDto>> GetBySemesterIdAsync(Guid semesterId);
    Task<ExamDto> CreateAsync(string title, string? description, Guid subjectId, Guid semesterId, DateTime examDate, int durationMinutes, decimal totalMarks);
    Task<ExamDto> UpdateAsync(Guid id, string title, string? description, Guid subjectId, Guid semesterId, DateTime examDate, int durationMinutes, decimal totalMarks);
    Task<bool> DeleteAsync(Guid id);

    // Rubric Management
    Task<RubricItemDto> AddRubricItemAsync(Guid examId, string criteria, string? description, decimal maxPoints);
    Task<IEnumerable<RubricItemDto>> AddRubricItemsAsync(Guid examId, IEnumerable<(string Criteria, string? Description, decimal MaxPoints)> rubricItems);
    Task<RubricItemDto> UpdateRubricItemAsync(Guid examId, Guid rubricItemId, string criteria, string? description, decimal maxPoints);
    Task<bool> RemoveRubricItemAsync(Guid examId, Guid rubricItemId);

    // Publishing
    Task<ExamDto> PublishExamAsync(Guid examId);

    // Examiner Assignment
    Task<ExaminerAssignment> AssignExaminerAsync(Guid examSessionId, Guid examinerId, string role = "Examiner");
    Task<bool> RemoveExaminerAssignmentAsync(Guid examSessionId, Guid examinerId);
}
