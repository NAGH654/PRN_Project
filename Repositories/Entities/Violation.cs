using System;
using Repositories.Entities.Enums;

namespace Repositories.Entities
{
	public class Violation
	{
		public Guid ViolationId { get; set; }
		public Guid SubmissionId { get; set; }
		public ViolationType ViolationType { get; set; }
		public string Description { get; set; } = string.Empty;
		public ViolationSeverity Severity { get; set; } = ViolationSeverity.Warning;
		public DateTime DetectedAt { get; set; }

		public Submission? Submission { get; set; }
	}
}


