using CoreService.Data;
using CoreService.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreService.Repositories;

public class SemesterRepository : ISemesterRepository
{
    private readonly CoreDbContext _context;

    public SemesterRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<Semester?> GetByIdAsync(Guid id)
    {
        return await _context.Semesters.FindAsync(id);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Semesters.AnyAsync(s => s.Id == id);
    }

    public async Task<bool> CodeExistsAsync(string code)
    {
        return await _context.Semesters.AnyAsync(s => s.Code == code);
    }

    public async Task<IEnumerable<Semester>> GetAllAsync()
    {
        return await _context.Semesters.ToListAsync();
    }

    public async Task<Semester> CreateAsync(Semester semester)
    {
        _context.Semesters.Add(semester);
        await _context.SaveChangesAsync();
        return semester;
    }

    public async Task<Semester> UpdateAsync(Semester semester)
    {
        _context.Semesters.Update(semester);
        await _context.SaveChangesAsync();
        return semester;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var semester = await GetByIdAsync(id);
        if (semester == null)
            return false;

        _context.Semesters.Remove(semester);
        await _context.SaveChangesAsync();
        return true;
    }
}