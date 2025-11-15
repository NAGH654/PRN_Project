namespace StorageService.Models;

public class ProcessingResult
{
    public string JobId { get; set; } = string.Empty;
    public string UploadPath { get; set; } = string.Empty;
    public string ExtractPath { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int SkippedFiles { get; set; }
    public int DuplicateFiles { get; set; }
    public int ErrorFiles { get; set; }
    public string Message { get; set; } = string.Empty;
}
