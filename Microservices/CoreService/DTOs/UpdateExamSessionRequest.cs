namespace CoreService.DTOs;

public class UpdateExamSessionRequest
{
    public string SessionName { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public string? Location { get; set; }
    public int MaxStudents { get; set; }
    public bool IsActive { get; set; } = true;
}