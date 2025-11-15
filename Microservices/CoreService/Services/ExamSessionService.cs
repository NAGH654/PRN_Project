using CoreService.Entities;
using CoreService.Repositories;

namespace CoreService.Services;

public class ExamSessionService : IExamSessionService
{
    private readonly IExamSessionRepository _repository;

    public ExamSessionService(IExamSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<ExamSession?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<ExamSession>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<ExamSession>> GetActiveAsync()
    {
        return await _repository.GetActiveAsync();
    }

    public async Task<IEnumerable<ExamSession>> GetByExamIdAsync(Guid examId)
    {
        return await _repository.GetByExamIdAsync(examId);
    }

    public async Task<ExamSession> CreateAsync(ExamSession session)
    {
        // Validation logic
        if (string.IsNullOrWhiteSpace(session.SessionName))
            throw new ArgumentException("Session name is required");

        if (session.MaxStudents <= 0)
            throw new ArgumentException("MaxStudents must be greater than 0");

        if (session.ScheduledDate < DateTime.UtcNow.Date)
            throw new ArgumentException("Scheduled date cannot be in the past");

        return await _repository.CreateAsync(session);
    }

    public async Task<ExamSession> UpdateAsync(Guid id, ExamSession session)
    {
        var existingSession = await _repository.GetByIdAsync(id);
        if (existingSession == null)
            throw new KeyNotFoundException($"ExamSession with id {id} not found");

        // Validation logic
        if (string.IsNullOrWhiteSpace(session.SessionName))
            throw new ArgumentException("Session name is required");

        if (session.MaxStudents <= 0)
            throw new ArgumentException("MaxStudents must be greater than 0");

        // Update properties
        existingSession.SessionName = session.SessionName;
        existingSession.ScheduledDate = session.ScheduledDate;
        existingSession.Location = session.Location;
        existingSession.MaxStudents = session.MaxStudents;
        existingSession.IsActive = session.IsActive;

        return await _repository.UpdateAsync(existingSession);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existingSession = await _repository.GetByIdAsync(id);
        if (existingSession == null)
            throw new KeyNotFoundException($"ExamSession with id {id} not found");

        await _repository.DeleteAsync(id);
    }
}
