using CoreService.Entities;

namespace CoreService.Services;

public interface ISemesterService
{
    Task<Semester?> GetByIdAsync(Guid id);
    Task<IEnumerable<Semester>> GetAllAsync();
    Task<Semester> CreateAsync(string name, DateTime startDate, DateTime endDate);
    Task<Semester> UpdateAsync(Guid id, string name, DateTime startDate, DateTime endDate);
    Task<bool> DeleteAsync(Guid id);
}