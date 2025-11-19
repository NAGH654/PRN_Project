using CoreService.Entities;
using CoreService.Repositories;

namespace CoreService.Services;

public class SemesterService : ISemesterService
{
    private readonly ISemesterRepository _semesterRepository;
    private readonly ILogger<SemesterService> _logger;

    public SemesterService(ISemesterRepository semesterRepository, ILogger<SemesterService> logger)
    {
        _semesterRepository = semesterRepository;
        _logger = logger;
    }

    public async Task<Semester?> GetByIdAsync(Guid id)
    {
        return await _semesterRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Semester>> GetAllAsync()
    {
        return await _semesterRepository.GetAllAsync();
    }

    public async Task<Semester> CreateAsync(string name, DateTime startDate, DateTime endDate)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Semester name is required", nameof(name));
        }

        if (startDate >= endDate)
        {
            throw new ArgumentException("Start date must be before end date", nameof(startDate));
        }

        if (startDate < DateTime.UtcNow.Date.AddYears(-1) || endDate > DateTime.UtcNow.Date.AddYears(2))
        {
            throw new ArgumentException("Semester dates must be within reasonable range", nameof(startDate));
        }

        // Check for duplicate code
        if (await _semesterRepository.CodeExistsAsync(name))
        {
            throw new InvalidOperationException($"Semester with code '{name}' already exists");
        }

        var semester = new Semester
        {
            Code = name,  // Set Code to the same value as Name
            Name = name,
            StartDate = startDate,
            EndDate = endDate
        };

        var created = await _semesterRepository.CreateAsync(semester);
        _logger.LogInformation("Semester '{Name}' created successfully", name);

        return created;
    }

    public async Task<Semester> UpdateAsync(Guid id, string name, DateTime startDate, DateTime endDate)
    {
        var semester = await _semesterRepository.GetByIdAsync(id);
        if (semester == null)
        {
            throw new KeyNotFoundException($"Semester with ID {id} not found");
        }

        // Validation
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Semester name is required", nameof(name));
        }

        if (startDate >= endDate)
        {
            throw new ArgumentException("Start date must be before end date", nameof(startDate));
        }

        // Check for duplicate code
        if (await _semesterRepository.CodeExistsAsync(name))
        {
            throw new InvalidOperationException($"Semester with code '{name}' already exists");
        }

        semester.Code = name;  // Update Code to match Name
        semester.Name = name;
        semester.StartDate = startDate;
        semester.EndDate = endDate;

        var updated = await _semesterRepository.UpdateAsync(semester);
        _logger.LogInformation("Semester {Id} updated successfully", id);

        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var deleted = await _semesterRepository.DeleteAsync(id);
        if (deleted)
        {
            _logger.LogInformation("Semester {Id} deleted successfully", id);
        }
        else
        {
            _logger.LogWarning("Semester {Id} not found for deletion", id);
        }

        return deleted;
    }
}