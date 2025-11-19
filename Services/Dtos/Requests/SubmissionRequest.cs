namespace Services.Dtos.Requests
{
    public class SubmissionRequest
    {
        public Guid SessionId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public string? ContentHash { get; set; }
    }
}

