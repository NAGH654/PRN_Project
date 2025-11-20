using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreService.Entities;

[Table("RubricScore", Schema = "Core")]
public class RubricScore
{
    [Key]
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; } // References Submission from StorageService

    public Guid RubricItemId { get; set; } // References RubricItem

    public Guid GradedBy { get; set; } // References User from IdentityService

    public decimal Points { get; set; } // Score given for this rubric item

    [MaxLength(1000)]
    public string? Comments { get; set; }

    public DateTime GradedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(RubricItemId))]
    public RubricItem RubricItem { get; set; } = null!;
}