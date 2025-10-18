using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

namespace Repositories.Entities
{
    public class RubricItem : IEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;
        [Required, MaxLength(32)] public string Code { get; set; } = null!;
        [Required, MaxLength(128)] public string Title { get; set; } = null!;
        [Precision(6, 2)] public decimal MaxPoints { get; set; }
        public bool AutoEvaluated { get; set; }
        [MaxLength(1000)] public string? Description { get; set; }
    }
}
