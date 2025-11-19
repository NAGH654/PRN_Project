namespace StorageService.Models;

public class UploadBatchForm
{
    public string? ExamId { get; set; }
    public IFormFile? Archive { get; set; }
}
