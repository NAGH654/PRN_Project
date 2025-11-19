using Microsoft.EntityFrameworkCore;
using StorageService.Data;
using StorageService.Entities;

namespace StorageService.Repositories;

public class SubmissionRepository : ISubmissionRepository
{
    private readonly StorageDbContext _context;

    public SubmissionRepository(StorageDbContext context)
    {
        _context = context;
    }

    public async Task<Submission?> GetByIdAsync(Guid id)
    {
        return await _context.Submissions
            .Include(s => s.Files)
            .Include(s => s.Violations)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Submission>> GetByStudentIdAsync(string studentId)
    {
        return await _context.Submissions
            .Include(s => s.Files)
            .Include(s => s.Violations)
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Submission>> GetByExamIdAsync(Guid examId)
    {
        return await _context.Submissions
            .Include(s => s.Files)
            .Include(s => s.Violations)
            .Where(s => s.ExamId == examId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Submission>> GetBySessionIdAsync(Guid sessionId)
    {
        return await _context.Submissions
            .Include(s => s.Files)
            .Include(s => s.Violations)
            .Where(s => s.ExamSessionId == sessionId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
    }

    public async Task<Submission?> GetByStudentAndExamAsync(string studentId, Guid examId)
    {
        return await _context.Submissions
            .Include(s => s.Files)
            .Include(s => s.Violations)
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.ExamId == examId);
    }

    public async Task<IEnumerable<Submission>> GetByStatusAsync(string status)
    {
        return await _context.Submissions
            .Include(s => s.Files)
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
    }

    public async Task<Submission> CreateAsync(Submission submission)
    {
        submission.Id = Guid.NewGuid();
        submission.CreatedAt = DateTime.UtcNow;
        submission.SubmittedAt = DateTime.UtcNow;
        
        _context.Submissions.Add(submission);
        await _context.SaveChangesAsync();
        
        return submission;
    }

    public async Task<Submission> UpdateAsync(Submission submission)
    {
        submission.UpdatedAt = DateTime.UtcNow;
        
        _context.Submissions.Update(submission);
        await _context.SaveChangesAsync();
        
        return submission;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var submission = await _context.Submissions.FindAsync(id);
        if (submission == null)
            return false;

        _context.Submissions.Remove(submission);
        await _context.SaveChangesAsync();
        
        return true;
    }
}
