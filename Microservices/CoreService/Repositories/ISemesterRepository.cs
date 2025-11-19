using CoreService.Entities;

namespace CoreService.Repositories;

public interface ISemesterRepository
{
    Task<Semester?> GetByIdAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> CodeExistsAsync(string code);
    Task<IEnumerable<Semester>> GetAllAsync();
    Task<Semester> CreateAsync(Semester semester);
    Task<Semester> UpdateAsync(Semester semester);
    Task<bool> DeleteAsync(Guid id);
}