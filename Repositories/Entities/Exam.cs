using System;
using System.Collections.Generic;

namespace Repositories.Entities
{
	public class Exam
	{
		public Guid ExamId { get; set; }
		public Guid SubjectId { get; set; }
		public Guid SemesterId { get; set; }
		public string ExamName { get; set; } = string.Empty;
		public DateTime ExamDate { get; set; }
		public int DurationMinutes { get; set; }
		public decimal TotalMarks { get; set; }
		public Guid CreatedBy { get; set; }
		public DateTime CreatedAt { get; set; }

		public Subject? Subject { get; set; }
		public Semester? Semester { get; set; }
		public User? CreatedByUser { get; set; }
		public ICollection<Rubric> Rubrics { get; set; } = new List<Rubric>();
		public ICollection<ExamSession> Sessions { get; set; } = new List<ExamSession>();
		public ICollection<ExaminerAssignment> ExaminerAssignments { get; set; } = new List<ExaminerAssignment>();
	}
}


