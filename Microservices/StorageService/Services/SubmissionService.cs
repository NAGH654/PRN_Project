using StorageService.Entities;
using StorageService.Repositories;

namespace StorageService.Services;

public class SubmissionService : ISubmissionService
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(
        ISubmissionRepository submissionRepository,
        ILogger<SubmissionService> logger)
    {
        _submissionRepository = submissionRepository;
        _logger = logger;
    }

    public async Task<Submission?> GetByIdAsync(Guid id)
    {
        return await _submissionRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Submission>> GetByStudentIdAsync(Guid studentId)
    {
        return await _submissionRepository.GetByStudentIdAsync(studentId);
    }

    public async Task<IEnumerable<Submission>> GetByExamIdAsync(Guid examId)
    {
        return await _submissionRepository.GetByExamIdAsync(examId);
    }

    public async Task<Submission> CreateSubmissionAsync(Guid studentId, Guid examId, Guid examSessionId)
    {
        // Check if student already has a submission for this exam
        var existing = await _submissionRepository.GetByStudentAndExamAsync(studentId, examId);
        if (existing != null)
        {
            _logger.LogWarning("Student {StudentId} already has submission for exam {ExamId}", studentId, examId);
            throw new InvalidOperationException("Submission already exists for this exam");
        }

        var submission = new Submission
        {
            StudentId = studentId,
            ExamId = examId,
            ExamSessionId = examSessionId,
            Status = "Pending",
            TotalFiles = 0,
            TotalSizeBytes = 0
        };

        var created = await _submissionRepository.CreateAsync(submission);
        _logger.LogInformation("Submission {Id} created for student {StudentId}", created.Id, studentId);
        
        return created;
    }

    public async Task<Submission> UpdateSubmissionStatusAsync(Guid id, string status, string? notes = null)
    {
        var submission = await _submissionRepository.GetByIdAsync(id);
        if (submission == null)
        {
            _logger.LogWarning("Submission {Id} not found", id);
            throw new KeyNotFoundException($"Submission with ID {id} not found");
        }

        var validStatuses = new[] { "Pending", "Processing", "Completed", "Failed" };
        if (!validStatuses.Contains(status))
        {
            _logger.LogWarning("Invalid status: {Status}", status);
            throw new ArgumentException($"Status must be one of: {string.Join(", ", validStatuses)}", nameof(status));
        }

        submission.Status = status;
        submission.ProcessingNotes = notes;
        
        if (status == "Completed" || status == "Failed")
        {
            submission.ProcessedAt = DateTime.UtcNow;
        }

        var updated = await _submissionRepository.UpdateAsync(submission);
        _logger.LogInformation("Submission {Id} status updated to {Status}", id, status);
        
        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var deleted = await _submissionRepository.DeleteAsync(id);
        if (deleted)
        {
            _logger.LogInformation("Submission {Id} deleted successfully", id);
        }
        else
        {
            _logger.LogWarning("Submission {Id} not found for deletion", id);
        }
        
        return deleted;
    }
}
