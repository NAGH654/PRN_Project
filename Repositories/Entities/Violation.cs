using System;

namespace Repositories.Entities
{
	public class Violation
	{
		public Guid ViolationId { get; set; }
		public Guid SubmissionId { get; set; }
		public string ViolationType { get; set; } = string.Empty; // Naming, Duplicate, Content
		public string Description { get; set; } = string.Empty;
		public string Severity { get; set; } = "Warning"; // Warning, Error
		public DateTime DetectedAt { get; set; }

		public Submission? Submission { get; set; }
	}
}


