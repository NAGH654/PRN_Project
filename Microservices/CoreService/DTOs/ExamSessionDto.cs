namespace CoreService.DTOs;

public class ExamSessionDto
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public string? Location { get; set; }
    public int MaxStudents { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ExaminerAssignmentsCount { get; set; }
}