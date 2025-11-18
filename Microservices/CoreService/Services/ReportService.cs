using CoreService.Data;
using CoreService.Models;
using Microsoft.EntityFrameworkCore;

namespace CoreService.Services;

public class ReportService : IReportService
{
    private readonly CoreDbContext _context;
    private readonly ILogger<ReportService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ReportService(CoreDbContext context, ILogger<ReportService> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
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

            var storageServiceUrl = _configuration["Services:StorageService"];

            var report = new List<ExamReportRow>();
            foreach (var e in exams)
            {
                // Call StorageService to get submission count
                int totalSubmissions = 0;
                try
                {
                    var response = await _httpClient.GetAsync($"{storageServiceUrl}/api/submissions/by-exam/{e.Id}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        // Assuming the response is a JSON array, count the items
                        // For simplicity, parse as list and count
                        var submissions = System.Text.Json.JsonSerializer.Deserialize<List<object>>(content);
                        totalSubmissions = submissions?.Count ?? 0;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get submissions for exam {ExamId}", e.Id);
                }

                report.Add(new ExamReportRow
                {
                    ExamId = e.Id,
                    ExamTitle = e.Title,
                    SubjectName = e.Subject?.Name ?? "Unknown",
                    ExamDate = e.ExamDate,
                    TotalSessions = e.ExamSessions.Count,
                    TotalGrades = e.Grades.Count,
                    TotalSubmissions = totalSubmissions,
                    AverageScore = e.Grades.Any() ? e.Grades.Average(g => g.Score) : 0,
                    HighestScore = e.Grades.Any() ? e.Grades.Max(g => g.Score) : 0,
                    LowestScore = e.Grades.Any() ? e.Grades.Min(g => g.Score) : 0
                });
            }

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating exam report");
            throw;
        }
    }
}
