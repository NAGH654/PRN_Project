using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreService.Entities;

[Table("Grade", Schema = "Core")]
public class Grade
{
    [Key]
    public Guid Id { get; set; }

    public Guid ExamId { get; set; }

    public Guid StudentId { get; set; } // References User from IdentityService

    public Guid? GradedBy { get; set; } // References User from IdentityService

    public decimal Score { get; set; }

    public decimal MaxScore { get; set; }

    [MaxLength(2000)]
    public string? Feedback { get; set; }

    public DateTime? GradedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ExamId))]
    public Exam Exam { get; set; } = null!;
}
