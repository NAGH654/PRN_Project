namespace StorageService.Models;

public class UploadBatchForm
{
    public string? SessionId { get; set; } // Changed from ExamId to match monolithic API
    public IFormFile? Archive { get; set; }
}
