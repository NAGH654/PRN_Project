namespace CoreService.DTOs;

public class GradingResponse
{
    public Guid SubmissionId { get; set; }
    public Guid GradedBy { get; set; }
    public string ExaminerName { get; set; } = string.Empty;
    public DateTime GradedAt { get; set; }
    public decimal TotalScore { get; set; }
    public decimal MaxScore { get; set; }
    public List<RubricScoreDetail> RubricScores { get; set; } = new();
    public GradingStatus Status { get; set; }
    public string? ModeratorReviewReason { get; set; }
    public bool RequiresModeratorReview { get; set; }
}

public class RubricScoreDetail
{
    public Guid RubricItemId { get; set; }
    public string Criteria { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Points { get; set; }
    public decimal MaxPoints { get; set; }
    public string? Comments { get; set; }
}

public enum GradingStatus
{
    FirstGrading,
    SecondGrading,
    AwaitingModeratorReview,
    Finalized
}