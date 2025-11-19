namespace Services.Dtos.Requests
{
    public class GradeRequest
    {
        public Guid SubmissionId { get; set; }
        public Guid RubricId { get; set; }
        public decimal Points { get; set; }
        public string? Comments { get; set; }
    }
}

