using CoreService.Models;

namespace CoreService.Services;

public interface IReportService
{
    Task<List<ExamReportRow>> GetExamReportAsync(Guid? subjectId = null, DateTime? fromDate = null, DateTime? toDate = null);
}
