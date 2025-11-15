using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageService.Entities;

[Table("Violation", Schema = "Storage")]
public class Violation
{
    [Key]
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty; // FileSizeExceeded, InvalidFormat, Plagiarism, etc.

    [Required]
    [MaxLength(50)]
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string? Details { get; set; } // JSON details

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public bool IsResolved { get; set; } = false;

    public DateTime? ResolvedAt { get; set; }

    public Guid? ResolvedBy { get; set; } // References User from IdentityService

    [MaxLength(1000)]
    public string? Resolution { get; set; }

    // Navigation properties
    [ForeignKey(nameof(SubmissionId))]
    public Submission Submission { get; set; } = null!;
}
