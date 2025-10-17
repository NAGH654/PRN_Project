using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models
{
    public class AssignmentDto
    {
        public Guid Id { get; set; }
        public Guid ClassId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? NamingRegex { get; set; }
        public string? KeywordsJson { get; set; }
        public string? RubricJson { get; set; }
        public DateTime? DueAt { get; set; }
    }

    public class AssignmentCreateDto
    {
        public Guid ClassId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? NamingRegex { get; set; }
        public string? KeywordsJson { get; set; }
        public string? RubricJson { get; set; }
        public DateTime? DueAt { get; set; }
    }

    public class AssignmentUpdateDto
    {
        public string? Name { get; set; }
        public string? NamingRegex { get; set; }
        public string? KeywordsJson { get; set; }
        public string? RubricJson { get; set; }
        public DateTime? DueAt { get; set; }
    }
}
