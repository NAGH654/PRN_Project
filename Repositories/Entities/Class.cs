using System.ComponentModel.DataAnnotations;
using Repositories.Interfaces;

namespace Repositories.Entities
{
    public class Class : IEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid CourseId { get; set; }
        public Course Course { get; set; } = null!;
        [Required, MaxLength(64)] public string Name { get; set; } = null!;
        [Required, MaxLength(32)] public string Term { get; set; } = null!;
        public Guid? LecturerId { get; set; }
        public User? Lecturer { get; set; }
    }
}
