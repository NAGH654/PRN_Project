using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

namespace Repositories.Entities
{
    public class Score : IEntity
    {
        [Key] public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        public Submission Submission { get; set; } = null!;
        [Precision(6, 2)] public decimal? P1 { get; set; }
        [Precision(6, 2)] public decimal? P2 { get; set; }
        [Precision(6, 2)] public decimal? P3 { get; set; }
        [Precision(6, 2)] public decimal FileNamePts { get; set; }
        [Precision(6, 2)] public decimal KeywordPts { get; set; }
        [Precision(6, 2)] public decimal ManualBonus { get; set; }
        public DateTime? GradedAt { get; set; }
        public Guid? GradedBy { get; set; }
        [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
