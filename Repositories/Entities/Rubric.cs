using System;
using System.Collections.Generic;

namespace Repositories.Entities
{
	public class Rubric
	{
		public Guid RubricId { get; set; }
		public Guid ExamId { get; set; }
		public string Criteria { get; set; } = string.Empty;
		public decimal MaxPoints { get; set; }
		public string? Description { get; set; }

		public Exam? Exam { get; set; }
		public ICollection<Grade> Grades { get; set; } = new List<Grade>();
	}
}


