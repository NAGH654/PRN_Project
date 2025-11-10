using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Services.Dtos.Requests
{
    public class UploadBatchForm
    {
        [Required]
        public Guid SessionId { get; set; }
        [Required]
        public IFormFile Archive { get; set; } = default!;
    }
}
