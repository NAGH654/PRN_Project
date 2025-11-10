using Services.Dtos.Responses;

namespace Services.Interfaces
{
    public interface IReportService
    {
        Task<(int total, List<SubmissionReportRow> data)> GetSubmissionsAsync(Guid? examId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default);
        Task<byte[]> ExportSubmissionsAsync(Guid? examId, DateTime? from, DateTime? to, CancellationToken ct = default);
        IQueryable<SubmissionReportODataRow> GetSubmissionsODataQueryable();
    }
}


