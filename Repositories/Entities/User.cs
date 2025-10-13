using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities.Enum;

namespace Repositories.Entities
{
    public class User
    {
        [Key] public Guid Id { get; set; }
        [Required, MaxLength(256)] public string Email { get; set; } = null!;
        [Required, MaxLength(128)] public string FullName { get; set; } = null!;
        public UserRole Role { get; set; }
        public string? PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
