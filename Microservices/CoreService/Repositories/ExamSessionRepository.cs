using CoreService.Data;
using CoreService.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreService.Repositories;

public class ExamSessionRepository : IExamSessionRepository
{
    private readonly CoreDbContext _context;

    public ExamSessionRepository(CoreDbContext context)
    {
        _context = context;
    }

    public async Task<ExamSession?> GetByIdAsync(Guid id)
    {
        return await _context.ExamSessions
            .Include(s => s.Exam)
            .Include(s => s.ExaminerAssignments)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<ExamSession>> GetAllAsync()
    {
        return await _context.ExamSessions
            .Include(s => s.Exam)
            .Include(s => s.ExaminerAssignments)
            .OrderByDescending(s => s.ScheduledDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<ExamSession>> GetActiveAsync()
    {
        return await _context.ExamSessions
            .Include(s => s.Exam)
            .Include(s => s.ExaminerAssignments)
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.ScheduledDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<ExamSession>> GetByExamIdAsync(Guid examId)
    {
        return await _context.ExamSessions
            .Include(s => s.Exam)
            .Include(s => s.ExaminerAssignments)
            .Where(s => s.ExamId == examId)
            .OrderByDescending(s => s.ScheduledDate)
            .ToListAsync();
    }

    public async Task<ExamSession> CreateAsync(ExamSession session)
    {
        session.Id = Guid.NewGuid();
        session.CreatedAt = DateTime.UtcNow;

        await _context.ExamSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        return session;
    }

    public async Task<ExamSession> UpdateAsync(ExamSession session)
    {
        _context.ExamSessions.Update(session);
        await _context.SaveChangesAsync();

        return session;
    }

    public async Task DeleteAsync(Guid id)
    {
        var session = await _context.ExamSessions.FindAsync(id);
        if (session != null)
        {
            _context.ExamSessions.Remove(session);
            await _context.SaveChangesAsync();
        }
    }
}
