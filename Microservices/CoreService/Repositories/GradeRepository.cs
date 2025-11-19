using Microsoft.EntityFrameworkCore;
using CoreService.Data;
using CoreService.Entities;

namespace CoreService.Repositories;

public class GradeRepository : IGradeRepository
{
    private readonly CoreDbContext _context;

    public GradeRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<Grade?> GetByIdAsync(Guid id)
    {
        return await _context.Grades
            .Include(g => g.Exam)
            .ThenInclude(e => e.Subject)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<IEnumerable<Grade>> GetByExamIdAsync(Guid examId)
    {
        return await _context.Grades
            .Include(g => g.Exam)
            .Where(g => g.ExamId == examId)
            .OrderByDescending(g => g.Score)
            .ToListAsync();
    }

    public async Task<IEnumerable<Grade>> GetByStudentIdAsync(Guid studentId)
    {
        return await _context.Grades
            .Include(g => g.Exam)
            .ThenInclude(e => e.Subject)
            .Where(g => g.StudentId == studentId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<Grade?> GetByExamAndStudentAsync(Guid examId, Guid studentId)
    {
        return await _context.Grades
            .Include(g => g.Exam)
            .FirstOrDefaultAsync(g => g.ExamId == examId && g.StudentId == studentId);
    }

    public async Task<Grade> CreateAsync(Grade grade)
    {
        grade.Id = Guid.NewGuid();
        grade.CreatedAt = DateTime.UtcNow;
        
        _context.Grades.Add(grade);
        await _context.SaveChangesAsync();
        
        return grade;
    }

    public async Task<Grade> UpdateAsync(Grade grade)
    {
        grade.UpdatedAt = DateTime.UtcNow;
        
        _context.Grades.Update(grade);
        await _context.SaveChangesAsync();
        
        return grade;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var grade = await _context.Grades.FindAsync(id);
        if (grade == null)
            return false;

        _context.Grades.Remove(grade);
        await _context.SaveChangesAsync();
        
        return true;
    }
}
