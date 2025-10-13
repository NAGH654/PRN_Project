using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Service
{
    public class ExportService : IExportService
    {
        private readonly AppDbContext _db;
        public ExportService(AppDbContext db) { _db = db; }

        public async Task<byte[]> ExportScoresExcelAsync(Guid assignmentId)
        {
            var rows = await _db.Scores
              .Include(s => s.Submission).ThenInclude(sub => sub.Student)
              .Where(s => s.Submission.AssignmentId == assignmentId)
              .Select(s => new {
                  StudentCode = s.Submission.Student.Code,
                  StudentName = s.Submission.Student.FullName,
                  P1 = s.P1,
                  P2 = s.P2,
                  P3 = s.P3,
                  FileNamePts = s.FileNamePts,
                  KeywordPts = s.KeywordPts,
                  Bonus = s.ManualBonus,
                  Total = (s.P1 ?? 0) + (s.P2 ?? 0) + (s.P3 ?? 0) + s.FileNamePts + s.KeywordPts + s.ManualBonus
              }).ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Scores");
            ws.Cell(1, 1).InsertTable(rows);
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
