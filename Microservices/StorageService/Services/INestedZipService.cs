using StorageService.Models;

namespace StorageService.Services;

public interface INestedZipService
{
    Task<ProcessingResult> ProcessNestedZipArchiveAsync(UploadBatchForm form, CancellationToken cancellationToken = default);
}
