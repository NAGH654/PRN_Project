namespace CoreService.DTOs;

public class AssignedExamResponse
{
    public Guid ExamId { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public Guid SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public int TotalSubmissions { get; set; }
    public int PendingSubmissions { get; set; }
    public int ProcessingSubmissions { get; set; }
    public int GradedSubmissions { get; set; }
    public int MyGradedSubmissions { get; set; }
}