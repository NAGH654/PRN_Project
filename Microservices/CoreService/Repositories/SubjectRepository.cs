using Microsoft.EntityFrameworkCore;
using CoreService.Data;
using CoreService.Entities;

namespace CoreService.Repositories;

public class SubjectRepository : ISubjectRepository
{
    private readonly CoreDbContext _context;

    public SubjectRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<Subject?> GetByIdAsync(Guid id)
    {
        return await _context.Subjects
            .Include(s => s.Exams)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Subject?> GetByCodeAsync(string code)
    {
        return await _context.Subjects
            .FirstOrDefaultAsync(s => s.Code == code);
    }

    public async Task<IEnumerable<Subject>> GetAllAsync()
    {
        return await _context.Subjects
            .OrderBy(s => s.Code)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Subject> CreateAsync(Subject subject)
    {
        subject.Id = Guid.NewGuid();
        subject.CreatedAt = DateTime.UtcNow;
        
        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();
        
        return subject;
    }

    public async Task<Subject> UpdateAsync(Subject subject)
    {
        subject.UpdatedAt = DateTime.UtcNow;
        
        _context.Subjects.Update(subject);
        await _context.SaveChangesAsync();
        
        return subject;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var subject = await _context.Subjects.FindAsync(id);
        if (subject == null)
            return false;

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();
        
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Subjects.AnyAsync(s => s.Id == id);
    }
}
