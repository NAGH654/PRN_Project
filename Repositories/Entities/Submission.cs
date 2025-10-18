using System.ComponentModel.DataAnnotations;
using Repositories.Entities.Enum;
using Repositories.Interfaces;

namespace Repositories.Entities
{
    public class Submission : IEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;
        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;
        [MaxLength(260)] public string? FolderName { get; set; }
        [MaxLength(260)] public string? OriginalArchive { get; set; }
        [MaxLength(260)] public string? MainDoc { get; set; }
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public SubmissionStatus Status { get; set; } = SubmissionStatus.New;
        [MaxLength(64)] public string? Sha256 { get; set; }
        [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Score? Score { get; set; }
        public ICollection<SubmissionFile> Files { get; set; } = new List<SubmissionFile>();
        public ICollection<Check> Checks { get; set; } = new List<Check>();
    }
}
