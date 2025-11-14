using CoreService.Entities;

namespace CoreService.Services;

public interface ISubjectService
{
    Task<Subject?> GetByIdAsync(Guid id);
    Task<Subject?> GetByCodeAsync(string code);
    Task<IEnumerable<Subject>> GetAllAsync();
    Task<Subject> CreateAsync(string code, string name, string? description, int credits);
    Task<Subject> UpdateAsync(Guid id, string code, string name, string? description, int credits);
    Task<bool> DeleteAsync(Guid id);
}
