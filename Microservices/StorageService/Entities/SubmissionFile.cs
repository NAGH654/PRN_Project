using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageService.Entities;

[Table("SubmissionFile", Schema = "Storage")]
public class SubmissionFile
{
    [Key]
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FileType { get; set; }

    public long FileSizeBytes { get; set; }

    [MaxLength(64)]
    public string? FileHash { get; set; } // SHA256 hash for integrity

    public bool IsImage { get; set; }

    public int? ImageWidth { get; set; }

    public int? ImageHeight { get; set; }

    [MaxLength(500)]
    public string? ThumbnailPath { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SubmissionId))]
    public Submission Submission { get; set; } = null!;
}
