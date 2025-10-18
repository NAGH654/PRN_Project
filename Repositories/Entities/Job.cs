using System.ComponentModel.DataAnnotations;
using Repositories.Entities.Enum;
using Repositories.Interfaces;

namespace Repositories.Entities
{
    public class Job : IEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;
        public Guid? UploaderId { get; set; }
        public User? Uploader { get; set; }
        public JobKind Kind { get; set; }
        public JobStatus Status { get; set; } = JobStatus.Queued;
        [Required, MaxLength(400)] public string InputPath { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public ICollection<JobItem> Items { get; set; } = new List<JobItem>();
    }
}
