using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public class Check
    {
        [Key] public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        public Submission Submission { get; set; } = null!;
        public bool FileNameOk { get; set; }
        [Precision(6, 2)] public decimal KeywordScore { get; set; }
        public string? KeywordHitsJson { get; set; }
        [MaxLength(1000)] public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
