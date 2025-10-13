namespace API.Request
{
    public class UploadBatchForm
    {
        public Guid AssignmentId { get; set; }
        public IFormFile Archive { get; set; } = default!;
    }
}
