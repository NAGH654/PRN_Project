using System.ComponentModel.DataAnnotations;
using Repositories.Interfaces;

namespace Repositories.Entities
{
    public class SubmissionFile : IEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        public Submission Submission { get; set; } = null!;
        [Required, MaxLength(400)] public string RelPath { get; set; } = null!;
        [Required, MaxLength(260)] public string FileName { get; set; } = null!;
        [MaxLength(10)] public string Ext { get; set; } = "";
        public long? SizeBytes { get; set; }
        [MaxLength(64)] public string? Sha256 { get; set; }
        public bool IsMainDoc { get; set; }
        public string? TextExcerpt { get; set; }
    }
}
