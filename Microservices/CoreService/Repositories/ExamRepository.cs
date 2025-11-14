using Microsoft.EntityFrameworkCore;
using CoreService.Data;
using CoreService.Entities;

namespace CoreService.Repositories;

public class ExamRepository : IExamRepository
{
    private readonly CoreDbContext _context;

    public ExamRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<Exam?> GetByIdAsync(Guid id)
    {
        return await _context.Exams
            .Include(e => e.Subject)
            .Include(e => e.Semester)
            .Include(e => e.RubricItems)
            .Include(e => e.ExamSessions)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<Exam>> GetAllAsync()
    {
        return await _context.Exams
            .Include(e => e.Subject)
            .Include(e => e.Semester)
            .OrderByDescending(e => e.ExamDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Exam>> GetBySubjectIdAsync(Guid subjectId)
    {
        return await _context.Exams
            .Include(e => e.Subject)
            .Include(e => e.Semester)
            .Where(e => e.SubjectId == subjectId)
            .OrderByDescending(e => e.ExamDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Exam>> GetBySemesterIdAsync(Guid semesterId)
    {
        return await _context.Exams
            .Include(e => e.Subject)
            .Include(e => e.Semester)
            .Where(e => e.SemesterId == semesterId)
            .OrderByDescending(e => e.ExamDate)
            .ToListAsync();
    }

    public async Task<Exam> CreateAsync(Exam exam)
    {
        exam.Id = Guid.NewGuid();
        exam.CreatedAt = DateTime.UtcNow;
        
        _context.Exams.Add(exam);
        await _context.SaveChangesAsync();
        
        return exam;
    }

    public async Task<Exam> UpdateAsync(Exam exam)
    {
        exam.UpdatedAt = DateTime.UtcNow;
        
        _context.Exams.Update(exam);
        await _context.SaveChangesAsync();
        
        return exam;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var exam = await _context.Exams.FindAsync(id);
        if (exam == null)
            return false;

        _context.Exams.Remove(exam);
        await _context.SaveChangesAsync();
        
        return true;
    }
}
