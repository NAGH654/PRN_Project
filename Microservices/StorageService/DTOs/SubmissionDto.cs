namespace StorageService.DTOs;

public class SubmissionDto
{
    public Guid Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public Guid ExamId { get; set; }
    public Guid ExamSessionId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime SubmittedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessingNotes { get; set; }
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int FilesCount { get; set; }
    public int ViolationsCount { get; set; }
}