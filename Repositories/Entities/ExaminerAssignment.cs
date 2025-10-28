using System;

namespace Repositories.Entities
{
	public class ExaminerAssignment
	{
		public Guid AssignmentId { get; set; }
		public Guid ExamId { get; set; }
		public Guid ExaminerId { get; set; }
		public Guid AssignedBy { get; set; }
		public DateTime AssignedAt { get; set; }

		public Exam? Exam { get; set; }
		public User? Examiner { get; set; }
		public User? AssignedByUser { get; set; }
	}
}


