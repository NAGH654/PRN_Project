using Repositories.Data;
using Repositories.Entities;
using Repositories.Entities.Enum;
using Services.Dtos;
using System;
using System.IO;                 // ⬅️ thêm
using System.Threading.Tasks;

namespace Services.Implement
{
    public class SubmissionService : ISubmissionService
    {
        private readonly AppDbContext _db;
        private readonly StorageOptions _opt;
        public SubmissionService(AppDbContext db, Microsoft.Extensions.Options.IOptions<StorageOptions> opt)
        {
            _db = db; _opt = opt.Value;
        }

        public async Task<UploadBatchResult> UploadBatchAsync(UploadBatchRequest req)
        {
            Directory.CreateDirectory(_opt.Root);

            var assignment = await _db.Assignments.FindAsync(req.AssignmentId)
                              ?? throw new InvalidOperationException("Assignment not found");

            var saveDir = Path.Combine(_opt.Root, "uploads", DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
            Directory.CreateDirectory(saveDir);

            var savePath = Path.Combine(saveDir, req.File.FileName);   // ⬅️ dùng File
            await using (var src = req.File.OpenReadStream())          // ⬅️ dùng OpenReadStream
            await using (var dst = File.Create(savePath))
            {
                await src.CopyToAsync(dst);
            }

            var job = new Job
            {
                AssignmentId = assignment.Id,
                Kind = JobKind.BatchUpload,
                Status = JobStatus.Queued,
                InputPath = savePath,
                UploaderId = req.UploaderId
            };

            await _db.Jobs.AddAsync(job);
            await _db.SaveChangesAsync();

            return new UploadBatchResult(job.Id);
        }
    }
}
