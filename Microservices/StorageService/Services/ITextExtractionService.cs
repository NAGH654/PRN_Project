using StorageService.Models;

namespace StorageService.Services;

public interface ITextExtractionService
{
    Task<SubmissionTextResponse?> GetSubmissionTextAsync(Guid submissionId, CancellationToken cancellationToken = default);
}
