using System;
using System.Collections.Generic;

namespace Repositories.Entities
{
	public class Subject
	{
		public Guid SubjectId { get; set; }
		public string SubjectCode { get; set; } = string.Empty;
		public string SubjectName { get; set; } = string.Empty;
		public string? Description { get; set; }
		public DateTime CreatedAt { get; set; }

		public ICollection<Exam> Exams { get; set; } = new List<Exam>();
	}
}


