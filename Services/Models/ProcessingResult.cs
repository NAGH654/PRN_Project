namespace Services.Models
{
    public class ProcessingResult
    {
        public int TotalFiles { get; set; }
        public int SubmissionsCreated { get; set; }
        public int ViolationsCreated { get; set; }
        public int ImagesExtracted { get; set; }
        public string JobId { get; set; } = string.Empty;
        public string UploadPath { get; set; } = string.Empty;
        public string ExtractPath { get; set; } = string.Empty;
        public List<CreatedSubmissionInfo> CreatedSubmissions { get; set; } = new();
    }

    public class CreatedSubmissionInfo
    {
        public Guid SubmissionId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string? StudentName { get; set; }
        public string FileName { get; set; } = string.Empty;
    }
}


