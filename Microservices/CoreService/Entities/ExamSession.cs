using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreService.Entities;

[Table("ExamSession", Schema = "Core")]
public class ExamSession
{
    [Key]
    public Guid Id { get; set; }

    public Guid ExamId { get; set; }

    [Required]
    [MaxLength(100)]
    public string SessionName { get; set; } = string.Empty;

    public DateTime ScheduledDate { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public int MaxStudents { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ExamId))]
    public Exam Exam { get; set; } = null!;

    public ICollection<ExaminerAssignment> ExaminerAssignments { get; set; } = new List<ExaminerAssignment>();
}
