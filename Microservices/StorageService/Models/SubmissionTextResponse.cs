namespace StorageService.Models;

public class SubmissionTextResponse
{
    public Guid SubmissionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public int CharCount { get; set; }
}
