namespace StorageService.Models;

public class ProcessingResult
{
    public string JobId { get; set; } = string.Empty;
    public string UploadPath { get; set; } = string.Empty;
    public string ExtractPath { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int SubmissionsCreated { get; set; }
    public int ViolationsCreated { get; set; }
    public int ImagesExtracted { get; set; }
    public int DuplicateFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int ErrorFiles { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<CreatedSubmissionInfo> CreatedSubmissions { get; set; } = new();
}

public class CreatedSubmissionInfo
{
    public Guid SubmissionId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
