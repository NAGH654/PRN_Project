using System;

namespace Services.Dtos.Responses
{
    public class SubmissionImageResponse
    {
        public Guid ImageId { get; set; }
        public string ImageName { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty; // relative to /files
        public long? ImageSize { get; set; }
    }
}


