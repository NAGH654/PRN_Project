using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

namespace Repositories.Entities
{
    public class AssignmentKeyword : IEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;
        [Required, MaxLength(256)] public string Phrase { get; set; } = null!;
        [Precision(6, 2)] public decimal Weight { get; set; }
        public bool IsRequired { get; set; } = false;
    }
}
