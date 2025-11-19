using CoreService.DTOs;
using CoreService.Entities;

namespace CoreService.Services;

public interface ISubjectService
{
    Task<SubjectDto?> GetByIdAsync(Guid id);
    Task<SubjectDto?> GetByCodeAsync(string code);
    Task<IEnumerable<SubjectDto>> GetAllAsync();
    Task<Subject> CreateAsync(string code, string name, string? description, int credits);
    Task<Subject> UpdateAsync(Guid id, string code, string name, string? description, int credits);
    Task<bool> DeleteAsync(Guid id);
}
