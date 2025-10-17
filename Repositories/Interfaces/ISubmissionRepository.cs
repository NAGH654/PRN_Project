using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface ISubmissionRepository : IGenericRepository<Submission>
    {
        Task<Submission?> GetByAssignmentAndStudentAsync(Guid assignmentId, Guid studentId);
    }
}
