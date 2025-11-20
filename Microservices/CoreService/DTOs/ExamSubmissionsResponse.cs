namespace CoreService.DTOs;

public class ExamSubmissionsResponse
{
    public List<SubmissionListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class SubmissionListItemDto
{
    public Guid SubmissionId { get; set; }
    public Guid SessionId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string? StudentName { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime SubmissionTime { get; set; }
    public string Status { get; set; } = string.Empty; // Submission status from StorageService
    public bool HasViolations { get; set; }
    public bool IsGradedByMe { get; set; }
    public bool IsGradedByOthers { get; set; }
    public int GradingCount { get; set; } // Number of examiners who have graded
    public bool RequiresDoubleGrading { get; set; }
    public decimal? CurrentScore { get; set; } // Latest score if graded
    public GradingStatus GradingStatus { get; set; }
}