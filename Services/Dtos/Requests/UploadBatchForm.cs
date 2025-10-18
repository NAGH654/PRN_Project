using Microsoft.AspNetCore.Http;

namespace Services.Dtos.Requests
{
    public class UploadBatchForm
    {
        public Guid AssignmentId { get; set; }
        public IFormFile Archive { get; set; } = default!;
    }
}
