using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public class Assignment
    {
        [Key] public Guid Id { get; set; }
        public Guid ClassId { get; set; }
        public Class Class { get; set; } = null!;
        [Required, MaxLength(32)] public string Code { get; set; } = null!;
        [Required, MaxLength(128)] public string Name { get; set; } = null!;
        public string? NamingRegex { get; set; }
        public string? KeywordsJson { get; set; }
        public string? RubricJson { get; set; }
        public DateTime? DueAt { get; set; }
        public ICollection<AssignmentKeyword> Keywords { get; set; } = new List<AssignmentKeyword>();
        public ICollection<RubricItem> RubricItems { get; set; } = new List<RubricItem>();
    }
}
