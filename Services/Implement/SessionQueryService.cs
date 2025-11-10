using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Services.Dtos.Responses;
using Services.Interfaces;

namespace Services.Implement
{
    public class SessionQueryService : ISessionQueryService
    {
        private readonly AppDbContext _db;

        public SessionQueryService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<ExamSessionResponse>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.ExamSessions
                .AsNoTracking()
                .Join(_db.Exams.AsNoTracking(), s => s.ExamId, e => e.ExamId, (s, e) => new ExamSessionResponse
                {
                    SessionId = s.SessionId,
                    ExamId = s.ExamId,
                    SessionName = s.SessionName,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsActive = s.IsActive,
                    ExamName = e.ExamName
                })
                .OrderByDescending(x => x.StartTime)
                .ToListAsync(ct);
        }

        public async Task<List<ExamSessionResponse>> GetActiveAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            return await _db.ExamSessions
                .AsNoTracking()
                .Where(s => s.IsActive && s.StartTime <= now && s.EndTime >= now)
                .Join(_db.Exams.AsNoTracking(), s => s.ExamId, e => e.ExamId, (s, e) => new ExamSessionResponse
                {
                    SessionId = s.SessionId,
                    ExamId = s.ExamId,
                    SessionName = s.SessionName,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsActive = s.IsActive,
                    ExamName = e.ExamName
                })
                .OrderByDescending(x => x.EndTime)
                .ToListAsync(ct);
        }
    }
}


