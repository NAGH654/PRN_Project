using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Services.Service
{
    public interface IUploadFile
    {
        string FileName { get; }
        string ContentType { get; }
        long Length { get; }
        Stream OpenReadStream();
    }
    public record UploadBatchRequest(Guid AssignmentId, IUploadFile File, Guid? UploaderId);
    public record UploadBatchResult(Guid JobId);

    public interface ISubmissionService
    {
        Task<UploadBatchResult> UploadBatchAsync(UploadBatchRequest req);
    }

    public interface IJobWorker
    {
        Task ProcessPendingJobsAsync(CancellationToken ct);
    }

    public interface IExportService
    {
        Task<byte[]> ExportScoresExcelAsync(Guid assignmentId);
    }
}
