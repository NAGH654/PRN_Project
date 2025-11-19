using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageService.Entities;

[Table("Submission", Schema = "Storage")]
public class Submission
{
    [Key]
    public Guid Id { get; set; }

    public string StudentId { get; set; } = string.Empty; // Student identifier (e.g., "SE171989")

    public Guid ExamId { get; set; } // References Exam from CoreService

    public Guid ExamSessionId { get; set; } // References ExamSession from CoreService

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    [MaxLength(2000)]
    public string? ProcessingNotes { get; set; }

    public int TotalFiles { get; set; }

    public long TotalSizeBytes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<SubmissionFile> Files { get; set; } = new List<SubmissionFile>();
    public ICollection<Violation> Violations { get; set; } = new List<Violation>();
    public ICollection<SubmissionImage> Images { get; set; } = new List<SubmissionImage>();
}
