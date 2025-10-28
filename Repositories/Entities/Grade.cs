using System;

namespace Repositories.Entities
{
	public class Grade
	{
		public Guid GradeId { get; set; }
		public Guid SubmissionId { get; set; }
		public Guid ExaminerId { get; set; }
		public Guid RubricId { get; set; }
		public decimal Points { get; set; }
		public string? Comments { get; set; }
		public DateTime GradedAt { get; set; }
		public bool IsFinal { get; set; }

		public Submission? Submission { get; set; }
		public User? Examiner { get; set; }
		public Rubric? Rubric { get; set; }
	}
}


