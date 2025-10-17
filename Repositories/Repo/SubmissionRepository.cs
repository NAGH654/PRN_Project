using Microsoft.EntityFrameworkCore;
using Repositories.Entities;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repo
{
    public class SubmissionRepository : GenericRepository<Submission>, ISubmissionRepository
    {
        public SubmissionRepository(Data.AppDbContext db) : base(db) { }
        public Task<Submission?> GetByAssignmentAndStudentAsync(Guid aId, Guid sId)
          => _set.Include(x => x.Files).Include(x => x.Score)
                 .FirstOrDefaultAsync(x => x.AssignmentId == aId && x.StudentId == sId);
    }
}
