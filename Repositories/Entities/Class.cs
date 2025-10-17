using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public class Class
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
