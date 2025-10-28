using System;

namespace Repositories.Entities
{
	public class SubmissionImage
	{
		public Guid ImageId { get; set; }
		public Guid SubmissionId { get; set; }
		public string ImageName { get; set; } = string.Empty;
		public string ImagePath { get; set; } = string.Empty;
		public long? ImageSize { get; set; }
		public DateTime ExtractedAt { get; set; }

		public Submission? Submission { get; set; }
	}
}


