using CoreService.Entities;
using CoreService.Repositories;

namespace CoreService.Services;

public class ExamService : IExamService
{
    private readonly IExamRepository _examRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<ExamService> _logger;

    public ExamService(
        IExamRepository examRepository,
        ISubjectRepository subjectRepository,
        ILogger<ExamService> logger)
    {
        _examRepository = examRepository;
        _subjectRepository = subjectRepository;
        _logger = logger;
    }

    public async Task<Exam?> GetByIdAsync(Guid id)
    {
        return await _examRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Exam>> GetAllAsync()
    {
        return await _examRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Exam>> GetBySubjectIdAsync(Guid subjectId)
    {
        return await _examRepository.GetBySubjectIdAsync(subjectId);
    }

    public async Task<IEnumerable<Exam>> GetBySemesterIdAsync(Guid semesterId)
    {
        return await _examRepository.GetBySemesterIdAsync(semesterId);
    }

    public async Task<Exam> CreateAsync(string title, string? description, Guid subjectId, Guid semesterId, DateTime examDate, int durationMinutes, decimal totalMarks)
    {
        // Validate subject exists
        var subjectExists = await _subjectRepository.ExistsAsync(subjectId);
        if (!subjectExists)
        {
            _logger.LogWarning("Subject {SubjectId} not found", subjectId);
            throw new KeyNotFoundException($"Subject with ID {subjectId} not found");
        }

        // Validate duration
        if (durationMinutes < 15 || durationMinutes > 360)
        {
            _logger.LogWarning("Invalid duration: {Duration}", durationMinutes);
            throw new ArgumentException("Duration must be between 15 and 360 minutes", nameof(durationMinutes));
        }

        // Validate total marks
        if (totalMarks <= 0 || totalMarks > 1000)
        {
            _logger.LogWarning("Invalid total marks: {TotalMarks}", totalMarks);
            throw new ArgumentException("Total marks must be between 0 and 1000", nameof(totalMarks));
        }

        var exam = new Exam
        {
            Title = title,
            Description = description,
            SubjectId = subjectId,
            SemesterId = semesterId,
            ExamDate = examDate,
            DurationMinutes = durationMinutes,
            TotalMarks = totalMarks
        };

        var created = await _examRepository.CreateAsync(exam);
        _logger.LogInformation("Exam {Title} created successfully", title);
        
        return created;
    }

    public async Task<Exam> UpdateAsync(Guid id, string title, string? description, Guid subjectId, Guid semesterId, DateTime examDate, int durationMinutes, decimal totalMarks)
    {
        var exam = await _examRepository.GetByIdAsync(id);
        if (exam == null)
        {
            _logger.LogWarning("Exam {Id} not found", id);
            throw new KeyNotFoundException($"Exam with ID {id} not found");
        }

        // Validate subject exists
        var subjectExists = await _subjectRepository.ExistsAsync(subjectId);
        if (!subjectExists)
        {
            _logger.LogWarning("Subject {SubjectId} not found", subjectId);
            throw new KeyNotFoundException($"Subject with ID {subjectId} not found");
        }

        // Validate duration
        if (durationMinutes < 15 || durationMinutes > 360)
        {
            _logger.LogWarning("Invalid duration: {Duration}", durationMinutes);
            throw new ArgumentException("Duration must be between 15 and 360 minutes", nameof(durationMinutes));
        }

        // Validate total marks
        if (totalMarks <= 0 || totalMarks > 1000)
        {
            _logger.LogWarning("Invalid total marks: {TotalMarks}", totalMarks);
            throw new ArgumentException("Total marks must be between 0 and 1000", nameof(totalMarks));
        }

        exam.Title = title;
        exam.Description = description;
        exam.SubjectId = subjectId;
        exam.SemesterId = semesterId;
        exam.ExamDate = examDate;
        exam.DurationMinutes = durationMinutes;
        exam.TotalMarks = totalMarks;

        var updated = await _examRepository.UpdateAsync(exam);
        _logger.LogInformation("Exam {Id} updated successfully", id);
        
        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var deleted = await _examRepository.DeleteAsync(id);
        if (deleted)
        {
            _logger.LogInformation("Exam {Id} deleted successfully", id);
        }
        else
        {
            _logger.LogWarning("Exam {Id} not found for deletion", id);
        }
        
        return deleted;
    }
}
