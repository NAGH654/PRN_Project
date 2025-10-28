using System;
using System.Collections.Generic;
using Repositories.Entities.Enums;

namespace Repositories.Entities
{
	public class Submission
	{
		public Guid SubmissionId { get; set; }
		public Guid SessionId { get; set; }
		public string StudentId { get; set; } = string.Empty;
		public string? StudentName { get; set; }
		public string FileName { get; set; } = string.Empty;
		public string FilePath { get; set; } = string.Empty;
		public long? FileSize { get; set; }
		public string? ContentHash { get; set; }
		public DateTime SubmissionTime { get; set; }
		public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

		public ExamSession? Session { get; set; }
		public ICollection<Violation> Violations { get; set; } = new List<Violation>();
		public ICollection<SubmissionImage> Images { get; set; } = new List<SubmissionImage>();
		public ICollection<Grade> Grades { get; set; } = new List<Grade>();
	}
}
