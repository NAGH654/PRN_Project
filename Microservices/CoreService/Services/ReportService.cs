using CoreService.Data;
using CoreService.Models;
using Microsoft.EntityFrameworkCore;

namespace CoreService.Services;

public class ReportService : IReportService
{
    private readonly CoreDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(CoreDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ExamReportRow>> GetExamReportAsync(Guid? subjectId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var query = _context.Exams
                .Include(e => e.Subject)
                .Include(e => e.ExamSessions)
                .Include(e => e.Grades)
                .AsQueryable();

            if (subjectId.HasValue)
            {
                query = query.Where(e => e.SubjectId == subjectId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(e => e.ExamDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(e => e.ExamDate <= toDate.Value);
            }

            var exams = await query.ToListAsync();

            var report = exams.Select(e => new ExamReportRow
            {
                ExamId = e.Id,
                ExamTitle = e.Title,
                SubjectName = e.Subject?.Name ?? "Unknown",
                ExamDate = e.ExamDate,
                TotalSessions = e.ExamSessions.Count,
                TotalGrades = e.Grades.Count,
                AverageScore = e.Grades.Any() ? e.Grades.Average(g => g.Score) : 0,
                HighestScore = e.Grades.Any() ? e.Grades.Max(g => g.Score) : 0,
                LowestScore = e.Grades.Any() ? e.Grades.Min(g => g.Score) : 0
            }).ToList();

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating exam report");
            throw;
        }
    }
}
