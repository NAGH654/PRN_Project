namespace StorageService.Models;

public class UploadBatchForm
{
    public string? SessionId { get; set; }
    public IFormFile? Archive { get; set; }
}
