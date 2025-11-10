using System.IO;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Services.Dtos.Responses;
using Services.Interfaces;

namespace Services.Implement
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _db;

        public ReportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<(int total, List<SubmissionReportRow> data)> GetSubmissionsAsync(Guid? examId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default)
        {
            // Use flattened query for counting and paging to avoid EF nested collection translation issues
            var flatQuery = BuildFlatRowsQuery(examId, from, to);
            var total = await flatQuery.CountAsync(ct);

            var pagedFlat = await flatQuery
                .OrderByDescending(x => x.SubmissionTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            // Load rubric averages only for the paged submissions
            var submissionIds = pagedFlat.Select(r => r.SubmissionId).ToList();
            var grades = _db.Grades.AsNoTracking();
            var rubrics = _db.Rubrics.AsNoTracking();

            var rubricAgg = await (from g in grades
                                   where submissionIds.Contains(g.SubmissionId)
                                   join r in rubrics on g.RubricId equals r.RubricId
                                   group new { g, r } by new { g.SubmissionId, r.RubricId, r.Criteria, r.MaxPoints } into grp
                                   select new
                                   {
                                       grp.Key.SubmissionId,
                                       Rubric = new RubricScoreResponse
                                       {
                                           RubricId = grp.Key.RubricId,
                                           Criteria = grp.Key.Criteria,
                                           MaxPoints = grp.Key.MaxPoints,
                                           AveragePoints = grp.Average(z => z.g.Points)
                                       }
                                   })
                                   .ToListAsync(ct);

            var subIdToRubrics = rubricAgg
                .GroupBy(x => x.SubmissionId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Rubric).ToList());

            var data = pagedFlat
                .Select(r => new SubmissionReportRow
                {
                    SubmissionId = r.SubmissionId,
                    ExamId = r.ExamId,
                    ExamName = r.ExamName,
                    SubjectName = r.SubjectName,
                    StudentId = r.StudentId,
                    StudentName = r.StudentName,
                    SubmissionTime = r.SubmissionTime,
                    HasViolations = r.HasViolations,
                    ViolationCount = r.ViolationCount,
                    RubricScores = subIdToRubrics.TryGetValue(r.SubmissionId, out var list) ? list : new List<RubricScoreResponse>(),
                    TotalAverageScore = (subIdToRubrics.TryGetValue(r.SubmissionId, out var l2) ? l2.Sum(x => x.AveragePoints) : 0)
                })
                .ToList();

            return (total, data);
        }

        public async Task<byte[]> ExportSubmissionsAsync(Guid? examId, DateTime? from, DateTime? to, CancellationToken ct = default)
        {
            // 1) Load base rows (scalar fields only) to avoid EF translating nested collections
            var baseRows = await BuildFlatRowsQuery(examId, from, to)
                .OrderBy(x => x.ExamName)
                .ThenBy(x => x.StudentId)
                .ToListAsync(ct);

            // 2) Fetch rubric averages for the selected submissions and attach in-memory
            var submissionIds = baseRows.Select(r => r.SubmissionId).ToList();
            var grades = _db.Grades.AsNoTracking();
            var rubrics = _db.Rubrics.AsNoTracking();

            var rubricAgg = await (from g in grades
                                   where submissionIds.Contains(g.SubmissionId)
                                   join r in rubrics on g.RubricId equals r.RubricId
                                   group new { g, r } by new { g.SubmissionId, r.RubricId, r.Criteria, r.MaxPoints } into grp
                                   select new
                                   {
                                       grp.Key.SubmissionId,
                                       Rubric = new RubricScoreResponse
                                       {
                                           RubricId = grp.Key.RubricId,
                                           Criteria = grp.Key.Criteria,
                                           MaxPoints = grp.Key.MaxPoints,
                                           AveragePoints = grp.Average(z => z.g.Points)
                                       }
                                   })
                                   .ToListAsync(ct);

            var subIdToRubrics = rubricAgg
                .GroupBy(x => x.SubmissionId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Rubric).ToList());

            var rows = baseRows
                .Select(r => new SubmissionReportRow
                {
                    SubmissionId = r.SubmissionId,
                    ExamId = r.ExamId,
                    ExamName = r.ExamName,
                    SubjectName = r.SubjectName,
                    StudentId = r.StudentId,
                    StudentName = r.StudentName,
                    SubmissionTime = r.SubmissionTime,
                    HasViolations = r.HasViolations,
                    ViolationCount = r.ViolationCount,
                    RubricScores = subIdToRubrics.TryGetValue(r.SubmissionId, out var list) ? list : new List<RubricScoreResponse>(),
                    TotalAverageScore = (subIdToRubrics.TryGetValue(r.SubmissionId, out var l2) ? l2.Sum(x => x.AveragePoints) : 0)
                })
                .ToList();

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Report");

            var baseHeaders = new[]
            {
                "Exam", "Subject", "SubmissionId", "StudentId", "StudentName", "SubmissionTime", "HasViolations", "ViolationCount", "TotalScore"
            };
            var rubricNames = rows
                .SelectMany(r => r.RubricScores)
                .Select(r => r.Criteria)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            var col = 1;
            foreach (var h in baseHeaders) ws.Cell(1, col++).Value = h;
            foreach (var rn in rubricNames) ws.Cell(1, col++).Value = rn;
            ws.Range(1,1,1,col-1).Style.Font.SetBold();

            var rowIdx = 2;
            foreach (var r in rows)
            {
                col = 1;
                ws.Cell(rowIdx, col++).Value = r.ExamName;
                ws.Cell(rowIdx, col++).Value = r.SubjectName;
                ws.Cell(rowIdx, col++).Value = r.SubmissionId.ToString();
                ws.Cell(rowIdx, col++).Value = r.StudentId;
                ws.Cell(rowIdx, col++).Value = r.StudentName;
                ws.Cell(rowIdx, col++).Value = r.SubmissionTime;
                ws.Cell(rowIdx, col++).Value = r.HasViolations;
                ws.Cell(rowIdx, col++).Value = r.ViolationCount;
                ws.Cell(rowIdx, col++).Value = r.TotalAverageScore;

                var rubricMap = r.RubricScores.ToDictionary(x => x.Criteria, x => x.AveragePoints);
                foreach (var rn in rubricNames)
                {
                    rubricMap.TryGetValue(rn, out var score);
                    ws.Cell(rowIdx, col++).Value = score;
                }
                rowIdx++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }

        public IQueryable<SubmissionReportODataRow> GetSubmissionsODataQueryable()
        {
            // Flattened projection for OData (no nested collections)
            var submissions = _db.Submissions.AsNoTracking();
            var sessions = _db.ExamSessions.AsNoTracking();
            var exams = _db.Exams.AsNoTracking();
            var subjects = _db.Subjects.AsNoTracking();
            var grades = _db.Grades.AsNoTracking();
            var rubrics = _db.Rubrics.AsNoTracking();
            var violations = _db.Violations.AsNoTracking();

            var baseQuery = from s in submissions
                            join sess in sessions on s.SessionId equals sess.SessionId
                            join e in exams on sess.ExamId equals e.ExamId
                            join subj in subjects on e.SubjectId equals subj.SubjectId
                            select new { s, e, subj };

            var odataQuery = from x in baseQuery
                              let totalAvg = (
                                  from g in grades
                                  where g.SubmissionId == x.s.SubmissionId
                                  join r in rubrics on g.RubricId equals r.RubricId
                                  group g by 1 into grp
                                  select grp.Average(z => z.Points)
                              ).FirstOrDefault()
                              let vCount = violations.Count(v => v.SubmissionId == x.s.SubmissionId)
                              select new SubmissionReportODataRow
                              {
                                  SubmissionId = x.s.SubmissionId,
                                  ExamId = x.e.ExamId,
                                  ExamName = x.e.ExamName,
                                  SubjectName = x.subj.SubjectName,
                                  StudentId = x.s.StudentId,
                                  StudentName = x.s.StudentName,
                                  SubmissionTime = x.s.SubmissionTime,
                                  HasViolations = vCount > 0,
                                  ViolationCount = vCount,
                                  TotalAverageScore = totalAvg
                              };

            return odataQuery;
        }

        private IQueryable<SubmissionReportRow> BuildReportQuery(Guid? examId, DateTime? from, DateTime? to)
        {
            var submissions = _db.Submissions.AsNoTracking();
            var sessions = _db.ExamSessions.AsNoTracking();
            var exams = _db.Exams.AsNoTracking();
            var subjects = _db.Subjects.AsNoTracking();
            var grades = _db.Grades.AsNoTracking();
            var rubrics = _db.Rubrics.AsNoTracking();
            var violations = _db.Violations.AsNoTracking();

            var baseQuery = from s in submissions
                            join sess in sessions on s.SessionId equals sess.SessionId
                            join e in exams on sess.ExamId equals e.ExamId
                            join subj in subjects on e.SubjectId equals subj.SubjectId
                            select new { s, sess, e, subj };

            if (examId.HasValue) baseQuery = baseQuery.Where(x => x.e.ExamId == examId.Value);
            if (from.HasValue) baseQuery = baseQuery.Where(x => x.s.SubmissionTime >= from.Value);
            if (to.HasValue) baseQuery = baseQuery.Where(x => x.s.SubmissionTime <= to.Value);

            var reportQuery = from x in baseQuery
                              let rubricScores =
                                  from g in grades
                                  where g.SubmissionId == x.s.SubmissionId
                                  join r in rubrics on g.RubricId equals r.RubricId
                                  group new { g, r } by new { r.RubricId, r.Criteria, r.MaxPoints } into grp
                                  select new RubricScoreResponse
                                  {
                                      RubricId = grp.Key.RubricId,
                                      Criteria = grp.Key.Criteria,
                                      MaxPoints = grp.Key.MaxPoints,
                                      AveragePoints = grp.Average(z => z.g.Points)
                                  }
                              let totalAvg = rubricScores.Sum(rs => rs.AveragePoints)
                              let vCount = violations.Count(v => v.SubmissionId == x.s.SubmissionId)
                              select new SubmissionReportRow
                              {
                                  SubmissionId = x.s.SubmissionId,
                                  ExamId = x.e.ExamId,
                                  ExamName = x.e.ExamName,
                                  SubjectName = x.subj.SubjectName,
                                  StudentId = x.s.StudentId,
                                  StudentName = x.s.StudentName,
                                  SubmissionTime = x.s.SubmissionTime,
                                  HasViolations = vCount > 0,
                                  ViolationCount = vCount,
                                  TotalAverageScore = totalAvg,
                                  RubricScores = rubricScores.ToList()
                              };

            return reportQuery;
        }

        private IQueryable<SubmissionReportODataRow> BuildFlatRowsQuery(Guid? examId, DateTime? from, DateTime? to)
        {
            var submissions = _db.Submissions.AsNoTracking();
            var sessions = _db.ExamSessions.AsNoTracking();
            var exams = _db.Exams.AsNoTracking();
            var subjects = _db.Subjects.AsNoTracking();
            var violations = _db.Violations.AsNoTracking();
            var grades = _db.Grades.AsNoTracking();
            var rubrics = _db.Rubrics.AsNoTracking();

            var baseQuery = from s in submissions
                            join sess in sessions on s.SessionId equals sess.SessionId
                            join e in exams on sess.ExamId equals e.ExamId
                            join subj in subjects on e.SubjectId equals subj.SubjectId
                            select new { s, e, subj };

            if (examId.HasValue) baseQuery = baseQuery.Where(x => x.e.ExamId == examId.Value);
            if (from.HasValue) baseQuery = baseQuery.Where(x => x.s.SubmissionTime >= from.Value);
            if (to.HasValue) baseQuery = baseQuery.Where(x => x.s.SubmissionTime <= to.Value);

            var flat = from x in baseQuery
                       let totalAvg = (
                           from g in grades
                           where g.SubmissionId == x.s.SubmissionId
                           join r in rubrics on g.RubricId equals r.RubricId
                           group g by 1 into grp
                           select grp.Average(z => z.Points)
                       ).FirstOrDefault()
                       let vCount = violations.Count(v => v.SubmissionId == x.s.SubmissionId)
                       select new SubmissionReportODataRow
                       {
                           SubmissionId = x.s.SubmissionId,
                           ExamId = x.e.ExamId,
                           ExamName = x.e.ExamName,
                           SubjectName = x.subj.SubjectName,
                           StudentId = x.s.StudentId,
                           StudentName = x.s.StudentName,
                           SubmissionTime = x.s.SubmissionTime,
                           HasViolations = vCount > 0,
                           ViolationCount = vCount,
                           TotalAverageScore = totalAvg
                       };

            return flat;
        }
    }
}


