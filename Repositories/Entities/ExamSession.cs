using System;
using System.Collections.Generic;

namespace Repositories.Entities
{
	public class ExamSession
	{
		public Guid SessionId { get; set; }
		public Guid ExamId { get; set; }
		public string SessionName { get; set; } = string.Empty;
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public bool IsActive { get; set; }

		public Exam? Exam { get; set; }
		public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
	}
}


