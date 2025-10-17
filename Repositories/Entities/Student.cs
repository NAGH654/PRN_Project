using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public class Student
    {
        [Key] public Guid Id { get; set; }
        [Required, MaxLength(32)] public string Code { get; set; } = null!;
        [Required, MaxLength(128)] public string FullName { get; set; } = null!;
        [MaxLength(256)] public string? Email { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
