using System;
using System.Collections.Generic;

namespace Repositories.Entities
{
	public class User
	{
		public Guid UserId { get; set; }
		public string Username { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string PasswordHash { get; set; } = string.Empty;
		public string Role { get; set; } = string.Empty; // Admin, Manager, Moderator, Examiner
		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }

		public ICollection<Exam> CreatedExams { get; set; } = new List<Exam>();
		public ICollection<Grade> GivenGrades { get; set; } = new List<Grade>();
		public ICollection<ExaminerAssignment> ExaminerAssignments { get; set; } = new List<ExaminerAssignment>();
		public ICollection<ExaminerAssignment> AssignedExaminerAssignments { get; set; } = new List<ExaminerAssignment>();
		public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
	}
}

