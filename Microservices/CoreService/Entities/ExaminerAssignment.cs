using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreService.Entities;

[Table("ExaminerAssignment", Schema = "Core")]
public class ExaminerAssignment
{
    [Key]
    public Guid Id { get; set; }

    public Guid ExamSessionId { get; set; }

    public Guid ExaminerId { get; set; } // References User from IdentityService

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = string.Empty; // e.g., "Lead", "Assistant"

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey(nameof(ExamSessionId))]
    public ExamSession ExamSession { get; set; } = null!;
}
