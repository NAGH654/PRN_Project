using System.ComponentModel.DataAnnotations;
using Repositories.Interfaces;

namespace Repositories.Entities
{
    public class Enrollment : IEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid ClassId { get; set; }
        public Class Class { get; set; } = null!;
        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;
    }
}
