using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Services.Interfaces;
using Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implement
{
    public class JobService : IJobService
    {
        private readonly AppDbContext _db;
        public JobService(AppDbContext db) => _db = db;

        public async Task<JobDtos?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var j = await _db.Jobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return j is null ? null : new JobDtos(j.Id, (int)j.Kind, (int)j.Status, j.InputPath, j.CreatedAt, j.CompletedAt);
        }
    }
}
