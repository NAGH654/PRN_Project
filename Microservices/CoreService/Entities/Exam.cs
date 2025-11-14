using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreService.Entities;

[Table("Exam", Schema = "Core")]
public class Exam
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public Guid SubjectId { get; set; }

    public Guid SemesterId { get; set; }

    public DateTime ExamDate { get; set; }

    public int DurationMinutes { get; set; }

    public decimal TotalMarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(SubjectId))]
    public Subject Subject { get; set; } = null!;

    [ForeignKey(nameof(SemesterId))]
    public Semester Semester { get; set; } = null!;

    public ICollection<RubricItem> RubricItems { get; set; } = new List<RubricItem>();
    public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
