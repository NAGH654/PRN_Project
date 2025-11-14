using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreService.Entities;

[Table("RubricItem", Schema = "Core")]
public class RubricItem
{
    [Key]
    public Guid Id { get; set; }

    public Guid ExamId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Criteria { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public decimal MaxPoints { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ExamId))]
    public Exam Exam { get; set; } = null!;
}
