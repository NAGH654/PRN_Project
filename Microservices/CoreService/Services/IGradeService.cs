using CoreService.Entities;
using CoreService.DTOs;

namespace CoreService.Services;

public interface IGradeService
{
    // Legacy methods (keep for backward compatibility)
    Task<Grade?> GetByIdAsync(Guid id);
    Task<IEnumerable<Grade>> GetByExamIdAsync(Guid examId);
    Task<IEnumerable<Grade>> GetByStudentIdAsync(Guid studentId);
    Task<Grade> CreateOrUpdateGradeAsync(Guid examId, Guid studentId, decimal score, string? feedback, Guid gradedBy);
    Task<bool> DeleteAsync(Guid id);

    // New rubric-based grading methods (Use Case 3)
    Task<GradingResponse> GradeSubmissionAsync(GradingRequest request);
    Task<GradingResponse?> GetSubmissionGradesAsync(Guid submissionId);
    Task<ExamSubmissionsResponse> GetExamSubmissionsAsync(Guid examId, Guid examinerId, int pageNumber = 1, int pageSize = 20);
    Task<IEnumerable<GradingResponse>> GetExaminerAssignedSubmissionsAsync(Guid examinerId);
    Task<bool> FinalizeGradesAsync(Guid submissionId, Guid moderatorId);
    Task<IEnumerable<GradingResponse>> GetSubmissionsRequiringReviewAsync();

    // Additional grading APIs (migrated from monolithic)
    Task<List<AssignedExamResponse>> GetAssignedExamsAsync(Guid examinerId);
    Task<SubmissionDetailsResponse?> GetSubmissionDetailsAsync(Guid submissionId, Guid examinerId);
    Task<List<RubricResponseDto>> GetExamRubricsAsync(Guid examId);
    Task<GradeResponseDto?> UpdateGradeAsync(Guid gradeId, UpdateGradeRequest request, Guid examinerId);
    Task<GradingResultResponse> MarkZeroDueToViolationsAsync(Guid submissionId, MarkZeroRequest request, Guid examinerId);
    Task<GradingStatusResponse?> GetGradingStatusAsync(Guid submissionId);
    Task<bool> IsExaminerAssignedAsync(Guid submissionId, Guid examinerId);
    Task<bool> ValidateAllRubricsGradedAsync(Guid submissionId, Guid examinerId);
    Task<decimal?> CalculateAverageScoreAsync(Guid submissionId);
    Task<bool> RequiresModeratorReviewAsync(Guid submissionId);
}
