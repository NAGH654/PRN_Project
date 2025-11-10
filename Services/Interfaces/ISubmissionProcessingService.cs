using Services.Dtos.Requests;
using Services.Models;

namespace Services.Interfaces
{
    public interface ISubmissionProcessingService
    {
        Task<ProcessingResult> ProcessArchiveAsync(UploadBatchForm form, CancellationToken cancellationToken = default);
        Task<ProcessingResult> ProcessNestedZipArchiveAsync(UploadBatchForm form, CancellationToken cancellationToken = default);
    }
}


