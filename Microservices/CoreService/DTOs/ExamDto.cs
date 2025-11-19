namespace CoreService.DTOs;

public class ExamDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public Guid SemesterId { get; set; }
    public string SemesterCode { get; set; } = string.Empty;
    public DateTime ExamDate { get; set; }
    public int DurationMinutes { get; set; }
    public decimal TotalMarks { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<RubricItemDto> RubricItems { get; set; } = new List<RubricItemDto>();
    public int ExamSessionsCount { get; set; }
}