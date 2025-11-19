namespace CoreService.DTOs;

public class CreateExamSessionRequest
{
    public Guid ExamId { get; set; }
    public string SessionName { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public string? Location { get; set; }
    public int MaxStudents { get; set; }
    public bool IsActive { get; set; } = true;
}