using Services.Dtos.Requests;
using Services.Dtos.Responses;

namespace Services.Interfaces
{
    public interface IGradingService
    {

        Task<List<AssignedExamResponse>> GetAssignedExamsAsync(Guid examinerId, CancellationToken ct = default);

        Task<PagedSubmissionResponse> GetExamSubmissionsAsync(Guid examId, Guid examinerId, GetSubmissionsQuery query, CancellationToken ct = default);
        Task<SubmissionGradingDetailsResponse?> GetSubmissionDetailsAsync(Guid submissionId, Guid examinerId, CancellationToken ct = default);

        Task<List<RubricResponse>> GetExamRubricsAsync(Guid examId, CancellationToken ct = default);

        Task<GradingResultResponse> SubmitGradesAsync(GradingRequests request, Guid examinerId,  CancellationToken ct = default);

        Task<GradeResponse?> UpdateGradeAsync(Guid gradeId, UpdateGradeRequest request, Guid examinerId, CancellationToken ct = default);

        Task<GradingResultResponse> MarkZeroDueToViolationsAsync(MarkZeroRequest request, Guid examinerId, CancellationToken ct = default);

        Task<List<GradeResponse>> GetSubmissionGradesAsync(Guid submissionId, CancellationToken ct = default);

        Task<GradingStatusResponse?> GetGradingStatusAsync(Guid submissionId, CancellationToken ct = default);

        Task<bool> IsExaminerAssignedAsync(Guid submissionId, Guid examinerId, CancellationToken ct = default);

        Task<bool> ValidateAllRubricsGradedAsync(Guid submissionId, Guid examinerId, CancellationToken ct = default);

        Task<decimal?> CalculateAverageScoreAsync(Guid submissionId, CancellationToken ct = default);

        Task<bool> RequiresModeratorReviewAsync(Guid submissionId, CancellationToken ct = default);
    }
}

