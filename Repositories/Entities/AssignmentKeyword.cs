using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public class AssignmentKeyword
    {
        [Key] public Guid Id { get; set; }
        public Guid AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;
        [Required, MaxLength(256)] public string Phrase { get; set; } = null!;
        [Precision(6, 2)] public decimal Weight { get; set; }
        public bool IsRequired { get; set; } = false;
    }
}
