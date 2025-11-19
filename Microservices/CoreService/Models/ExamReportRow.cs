namespace CoreService.Models;

public class ExamReportRow
{
    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public DateTime ExamDate { get; set; }
    public int TotalSessions { get; set; }
    public int TotalGrades { get; set; }
    public int TotalSubmissions { get; set; }
    public decimal AverageScore { get; set; }
    public decimal HighestScore { get; set; }
    public decimal LowestScore { get; set; }
}
