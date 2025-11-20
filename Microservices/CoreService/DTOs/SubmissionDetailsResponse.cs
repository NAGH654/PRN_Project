namespace CoreService.DTOs;

public class SubmissionDetailsResponse
{
    public Guid SubmissionId { get; set; }
    public Guid SessionId { get; set; }
    public Guid ExamId { get; set; }
    public string ExamName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string? StudentName { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileDownloadUrl { get; set; } = string.Empty;
    public DateTime SubmissionTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<ViolationItemDto> Violations { get; set; } = new();
    public List<RubricGradingItemDto> Rubrics { get; set; } = new();
    public List<GradeResponseDto> ExistingGrades { get; set; } = new();
    public bool CanEdit { get; set; }
    public bool IsSecondGrader { get; set; }
}

public class RubricGradingItemDto
{
    public Guid RubricId { get; set; }
    public string Criteria { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MaxPoints { get; set; }
    public decimal? Points { get; set; }
    public string? Comments { get; set; }
}

public class ViolationItemDto
{
    public Guid ViolationId { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}

public class GradeResponseDto
{
    public Guid GradeId { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid ExaminerId { get; set; }
    public string? ExaminerName { get; set; }
    public Guid RubricId { get; set; }
    public string RubricCriteria { get; set; } = string.Empty;
    public decimal Points { get; set; }
    public decimal MaxPoints { get; set; }
    public string? Comments { get; set; }
    public DateTime GradedAt { get; set; }
    public bool IsFinal { get; set; }
}