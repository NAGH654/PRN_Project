using CoreService.Entities;

namespace CoreService.Repositories;

public interface ISubjectRepository
{
    Task<Subject?> GetByIdAsync(Guid id);
    Task<Subject?> GetByCodeAsync(string code);
    Task<IEnumerable<Subject>> GetAllAsync();
    Task<Subject> CreateAsync(Subject subject);
    Task<Subject> UpdateAsync(Subject subject);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
