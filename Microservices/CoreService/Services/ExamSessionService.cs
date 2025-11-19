using CoreService.Entities;
using CoreService.Repositories;
using CoreService.DTOs;
using Mapster;

namespace CoreService.Services;

public class ExamSessionService : IExamSessionService
{
    private readonly IExamSessionRepository _repository;

    public ExamSessionService(IExamSessionRepository repository)
    {
        _repository = repository;

        // Configure Mapster mappings
        ConfigureMappings();
    }

    private void ConfigureMappings()
    {
        TypeAdapterConfig<ExamSession, ExamSessionDto>
            .NewConfig()
            .Map(dest => dest.ExamTitle, src => src.Exam != null ? src.Exam.Title : string.Empty)
            .Map(dest => dest.ExaminerAssignmentsCount, src => src.ExaminerAssignments.Count);
    }

    public async Task<ExamSessionDto?> GetByIdAsync(Guid id)
    {
        var session = await _repository.GetByIdAsync(id);
        return session?.Adapt<ExamSessionDto>();
    }

    public async Task<IEnumerable<ExamSessionDto>> GetAllAsync()
    {
        var sessions = await _repository.GetAllAsync();
        return sessions.Adapt<IEnumerable<ExamSessionDto>>();
    }

    public async Task<IEnumerable<ExamSessionDto>> GetActiveAsync()
    {
        var sessions = await _repository.GetActiveAsync();
        return sessions.Adapt<IEnumerable<ExamSessionDto>>();
    }

    public async Task<IEnumerable<ExamSessionDto>> GetByExamIdAsync(Guid examId)
    {
        var sessions = await _repository.GetByExamIdAsync(examId);
        return sessions.Adapt<IEnumerable<ExamSessionDto>>();
    }

    public async Task<ExamSessionDto> CreateAsync(CreateExamSessionRequest request)
    {
        // Validation logic
        if (string.IsNullOrWhiteSpace(request.SessionName))
            throw new ArgumentException("Session name is required");

        if (request.MaxStudents <= 0)
            throw new ArgumentException("MaxStudents must be greater than 0");

        if (request.ScheduledDate < DateTime.UtcNow.Date)
            throw new ArgumentException("Scheduled date cannot be in the past");

        var session = new ExamSession
        {
            ExamId = request.ExamId,
            SessionName = request.SessionName,
            ScheduledDate = request.ScheduledDate,
            Location = request.Location,
            MaxStudents = request.MaxStudents,
            IsActive = request.IsActive
        };

        var created = await _repository.CreateAsync(session);
        return created.Adapt<ExamSessionDto>();
    }

    public async Task<ExamSessionDto> UpdateAsync(Guid id, UpdateExamSessionRequest request)
    {
        var existingSession = await _repository.GetByIdAsync(id);
        if (existingSession == null)
            throw new KeyNotFoundException($"ExamSession with id {id} not found");

        // Validation logic
        if (string.IsNullOrWhiteSpace(request.SessionName))
            throw new ArgumentException("Session name is required");

        if (request.MaxStudents <= 0)
            throw new ArgumentException("MaxStudents must be greater than 0");

        // Update properties
        existingSession.SessionName = request.SessionName;
        existingSession.ScheduledDate = request.ScheduledDate;
        existingSession.Location = request.Location;
        existingSession.MaxStudents = request.MaxStudents;
        existingSession.IsActive = request.IsActive;

        var updated = await _repository.UpdateAsync(existingSession);
        return updated.Adapt<ExamSessionDto>();
    }

    public async Task DeleteAsync(Guid id)
    {
        var existingSession = await _repository.GetByIdAsync(id);
        if (existingSession == null)
            throw new KeyNotFoundException($"ExamSession with id {id} not found");

        await _repository.DeleteAsync(id);
    }
}
