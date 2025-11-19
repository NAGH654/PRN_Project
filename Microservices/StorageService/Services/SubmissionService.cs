using StorageService.Entities;
using StorageService.Repositories;
using StorageService.DTOs;
using Mapster;

namespace StorageService.Services;

public class SubmissionService : ISubmissionService
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly ILogger<SubmissionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public SubmissionService(
        ISubmissionRepository submissionRepository,
        ILogger<SubmissionService> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _submissionRepository = submissionRepository;
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;

        // Configure Mapster mappings
        ConfigureMappings();
    }

    private void ConfigureMappings()
    {
        TypeAdapterConfig<Submission, SubmissionDto>
            .NewConfig()
            .Map(dest => dest.FilesCount, src => src.Files.Count)
            .Map(dest => dest.ViolationsCount, src => src.Violations.Count);
    }

    public async Task<SubmissionDto?> GetByIdAsync(Guid id)
    {
        var submission = await _submissionRepository.GetByIdAsync(id);
        return submission?.Adapt<SubmissionDto>();
    }

    public async Task<IEnumerable<SubmissionDto>> GetByStudentIdAsync(string studentId)
    {
        var submissions = await _submissionRepository.GetByStudentIdAsync(studentId);
        return submissions.Adapt<IEnumerable<SubmissionDto>>();
    }

    public async Task<IEnumerable<SubmissionDto>> GetByExamIdAsync(Guid examId)
    {
        var submissions = await _submissionRepository.GetByExamIdAsync(examId);
        return submissions.Adapt<IEnumerable<SubmissionDto>>();
    }

    public async Task<IEnumerable<SubmissionDto>> GetBySessionIdAsync(Guid sessionId)
    {
        var submissions = await _submissionRepository.GetBySessionIdAsync(sessionId);
        return submissions.Adapt<IEnumerable<SubmissionDto>>();
    }

    public async Task<SubmissionDto> CreateSubmissionAsync(string studentId, Guid examId, Guid examSessionId)
    {
        // Validate exam exists by calling CoreService
        var coreServiceUrl = _configuration["Services:CoreService"];
        var examResponse = await _httpClient.GetAsync($"{coreServiceUrl}/api/exams/{examId}");
        if (!examResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Exam {ExamId} not found in CoreService", examId);
            throw new InvalidOperationException("Invalid exam ID");
        }

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

        return created.Adapt<SubmissionDto>();
    }

    public async Task<SubmissionDto> UpdateSubmissionStatusAsync(Guid id, string status, string? notes = null)
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

        return updated.Adapt<SubmissionDto>();
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
