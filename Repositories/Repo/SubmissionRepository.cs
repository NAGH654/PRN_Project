using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using Repositories.Interfaces;

namespace Repositories.Repo
{
    public class SubmissionRepository(AppDbContext db) : GenericRepository<Submission>(db), ISubmissionRepository
    {
        public Task<Submission?> GetByAssignmentAndStudentAsync(Guid aId, Guid sId)
          => _set.Include(x => x.Files)
          .Include(x => x.Score)
          .FirstOrDefaultAsync(x => x.AssignmentId == aId && x.StudentId == sId);
    }
}
