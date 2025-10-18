using System.ComponentModel.DataAnnotations;
using Repositories.Interfaces;

namespace Repositories.Entities
{
    public class JobItem : IEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid JobId { get; set; }
        public Job Job { get; set; } = null!;
        public Guid? SubmissionId { get; set; }
        public Submission? Submission { get; set; }
        public byte State { get; set; } // 0 pend,1 ok,2 skip,3 fail
        [MaxLength(1000)] public string? Message { get; set; }
    }
}
