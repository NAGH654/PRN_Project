namespace CoreService.DTOs;

public class GradingRequest
{
    public Guid SubmissionId { get; set; }
    public Guid GradedBy { get; set; }
    public List<RubricScoreRequest> RubricScores { get; set; } = new();
}

public class RubricScoreRequest
{
    public Guid RubricItemId { get; set; }
    public decimal Points { get; set; }
    public string? Comments { get; set; }
}