using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageService.Entities;

[Table("SubmissionImage", Schema = "Storage")]
public class SubmissionImage
{
    [Key]
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    [Required]
    [MaxLength(255)]
    public string ImageName { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string ImagePath { get; set; } = string.Empty;

    public long ImageSizeBytes { get; set; }

    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(SubmissionId))]
    public Submission Submission { get; set; } = null!;
}