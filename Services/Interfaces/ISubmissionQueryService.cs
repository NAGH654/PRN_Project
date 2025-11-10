using Services.Dtos.Responses;

namespace Services.Interfaces
{
    public interface ISubmissionQueryService
    {
        Task<List<SubmissionImageResponse>> GetSubmissionImagesAsync(Guid submissionId, CancellationToken ct = default);
        Task<List<SubmissionStudentItem>> GetSessionStudentsAsync(Guid sessionId, CancellationToken ct = default);
        Task<SubmissionTextResponse?> GetSubmissionTextAsync(Guid submissionId, CancellationToken ct = default);
    }
}


