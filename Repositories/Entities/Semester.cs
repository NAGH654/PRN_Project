using System;
using System.Collections.Generic;

namespace Repositories.Entities
{
	public class Semester
	{
		public Guid SemesterId { get; set; }
		public string SemesterCode { get; set; } = string.Empty;
		public string SemesterName { get; set; } = string.Empty;
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public bool IsActive { get; set; }

		public ICollection<Exam> Exams { get; set; } = new List<Exam>();
	}
}


