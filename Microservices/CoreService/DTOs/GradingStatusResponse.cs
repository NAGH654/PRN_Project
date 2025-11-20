namespace CoreService.DTOs;

public class GradingStatusResponse
{
    public Guid SubmissionId { get; set; }
    public int ExaminerCount { get; set; }
    public bool RequiresDoubleGrading { get; set; }
    public bool IsDoubleGradingComplete { get; set; }
    public decimal? AverageScore { get; set; }
    public decimal? MaxPossibleScore { get; set; }
    public bool RequiresModeratorReview { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<ExaminerGradingSummaryDto> ExaminerSummaries { get; set; } = new();
}

public class ExaminerGradingSummaryDto
{
    public Guid ExaminerId { get; set; }
    public string? ExaminerName { get; set; }
    public decimal TotalScore { get; set; }
    public int RubricsGraded { get; set; }
    public DateTime? LastGradedAt { get; set; }
}

public class GradingResultResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid SubmissionId { get; set; }
    public string? NewStatus { get; set; }
    public bool RequiresModeratorReview { get; set; }
    public decimal? AverageScore { get; set; }
    public decimal? ScoreDifference { get; set; }
    public List<GradeResponseDto> CreatedGrades { get; set; } = new();
}

public class RubricResponseDto
{
    public Guid RubricId { get; set; }
    public Guid ExamId { get; set; }
    public string Criteria { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MaxPoints { get; set; }
}

public class UpdateGradeRequest
{
    public decimal Points { get; set; }
    public string? Comments { get; set; }
}

public class MarkZeroRequest
{
    public string Reason { get; set; } = string.Empty;
}