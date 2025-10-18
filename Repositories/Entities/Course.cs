using System.ComponentModel.DataAnnotations;
using Repositories.Interfaces;

namespace Repositories.Entities
{
    public class Course : IEntity
    {
        [Key] public Guid Id { get; set; }
        [Required, MaxLength(32)] public string Code { get; set; } = null!;
        [Required, MaxLength(128)] public string Name { get; set; } = null!;
    }
}
