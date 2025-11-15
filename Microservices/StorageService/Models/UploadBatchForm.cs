using System.ComponentModel.DataAnnotations;

namespace StorageService.Models;

public class UploadBatchForm
{
    [Required]
    public Guid SessionId { get; set; }
    
    [Required]
    public IFormFile Archive { get; set; } = default!;
}
