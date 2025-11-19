namespace CoreService.DTOs;

public class RubricItemDto
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public string Criteria { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MaxPoints { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}