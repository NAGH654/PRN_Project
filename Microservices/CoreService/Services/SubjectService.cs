using CoreService.DTOs;
using CoreService.Entities;
using CoreService.Repositories;
using Mapster;

namespace CoreService.Services;

public class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<SubjectService> _logger;

    public SubjectService(ISubjectRepository subjectRepository, ILogger<SubjectService> logger)
    {
        _subjectRepository = subjectRepository;
        _logger = logger;

        // Configure Mapster mappings
        ConfigureMappings();
    }

    private void ConfigureMappings()
    {
        // Basic mapping - all properties map by name/convention
        TypeAdapterConfig<Subject, SubjectDto>.NewConfig();
    }

    public async Task<SubjectDto?> GetByIdAsync(Guid id)
    {
        var subject = await _subjectRepository.GetByIdAsync(id);
        return subject?.Adapt<SubjectDto>();
    }

    public async Task<SubjectDto?> GetByCodeAsync(string code)
    {
        var subject = await _subjectRepository.GetByCodeAsync(code);
        return subject?.Adapt<SubjectDto>();
    }

    public async Task<IEnumerable<SubjectDto>> GetAllAsync()
    {
        var subjects = await _subjectRepository.GetAllAsync();
        return subjects.Adapt<IEnumerable<SubjectDto>>();
    }

    public async Task<Subject> CreateAsync(string code, string name, string? description, int credits)
    {
        // Validate code is unique
        var existingSubject = await _subjectRepository.GetByCodeAsync(code);
        if (existingSubject != null)
        {
            _logger.LogWarning("Subject with code {Code} already exists", code);
            throw new InvalidOperationException($"Subject with code '{code}' already exists");
        }

        // Validate credits
        if (credits < 1 || credits > 10)
        {
            _logger.LogWarning("Invalid credits value: {Credits}", credits);
            throw new ArgumentException("Credits must be between 1 and 10", nameof(credits));
        }

        var subject = new Subject
        {
            Code = code,
            Name = name,
            Description = description,
            Credits = credits
        };

        var created = await _subjectRepository.CreateAsync(subject);
        _logger.LogInformation("Subject {Code} created successfully", code);
        
        return created;
    }

    public async Task<Subject> UpdateAsync(Guid id, string code, string name, string? description, int credits)
    {
        var subject = await _subjectRepository.GetByIdAsync(id);
        if (subject == null)
        {
            _logger.LogWarning("Subject {Id} not found", id);
            throw new KeyNotFoundException($"Subject with ID {id} not found");
        }

        // Validate code is unique (excluding current subject)
        var existingSubject = await _subjectRepository.GetByCodeAsync(code);
        if (existingSubject != null && existingSubject.Id != id)
        {
            _logger.LogWarning("Subject with code {Code} already exists", code);
            throw new InvalidOperationException($"Subject with code '{code}' already exists");
        }

        // Validate credits
        if (credits < 1 || credits > 10)
        {
            _logger.LogWarning("Invalid credits value: {Credits}", credits);
            throw new ArgumentException("Credits must be between 1 and 10", nameof(credits));
        }

        subject.Code = code;
        subject.Name = name;
        subject.Description = description;
        subject.Credits = credits;

        var updated = await _subjectRepository.UpdateAsync(subject);
        _logger.LogInformation("Subject {Id} updated successfully", id);
        
        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var exists = await _subjectRepository.ExistsAsync(id);
        if (!exists)
        {
            _logger.LogWarning("Subject {Id} not found for deletion", id);
            return false;
        }

        var deleted = await _subjectRepository.DeleteAsync(id);
        if (deleted)
        {
            _logger.LogInformation("Subject {Id} deleted successfully", id);
        }
        
        return deleted;
    }
}
