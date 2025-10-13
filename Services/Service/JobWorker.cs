using Microsoft.Extensions.DependencyInjection;
using Repositories.Data;
using Repositories.Entities.Enum;
using Repositories.Entities;
using Services.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using Microsoft.Extensions.Hosting;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;


namespace Services.Service
{
    public class JobWorker : BackgroundService, IJobWorker
    {
        private readonly IServiceProvider _sp;
        private readonly StorageOptions _opt;
        public JobWorker(IServiceProvider sp, Microsoft.Extensions.Options.IOptions<StorageOptions> opt)
        {
            _sp = sp; _opt = opt.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await ProcessPendingJobsAsync(stoppingToken); }
                catch (Exception ex) { Console.WriteLine(ex); }
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        public async Task ProcessPendingJobsAsync(CancellationToken ct)
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var job = await db.Jobs.Include(j => j.Assignment)
                         .Where(j => j.Status == JobStatus.Queued)
                         .OrderBy(j => j.CreatedAt).FirstOrDefaultAsync(ct);
            if (job == null) return;

            job.Status = JobStatus.Running;
            await db.SaveChangesAsync(ct);

            var staging = Path.Combine(_opt.Root, "jobs", job.Id.ToString());
            Directory.CreateDirectory(staging);

            try
            {
                var extracted = await ExtractArchiveAsync(job.InputPath, staging, ct);

                // Giải các solution.zip lồng bên trong (nếu có)
                var allFiles = Directory.EnumerateFiles(extracted, "*.*", SearchOption.AllDirectories).ToList();
                foreach (var file in allFiles.Where(f => Path.GetExtension(f).Equals(".zip", StringComparison.OrdinalIgnoreCase)))
                {
                    var unzipTo = Path.Combine(Path.GetDirectoryName(file)!, Path.GetFileNameWithoutExtension(file));
                    Directory.CreateDirectory(unzipTo);
                    ZipFile.ExtractToDirectory(file, unzipTo, overwriteFiles: true);
                }

                var docxFiles = Directory.EnumerateFiles(extracted, "*.docx", SearchOption.AllDirectories);
                foreach (var docx in docxFiles)
                {
                    await HandleDocxAsync(db, job, docx, ct);
                }

                job.Status = JobStatus.Done;
                job.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                job.Status = JobStatus.Failed;
                job.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                Console.WriteLine("Job failed: " + ex);
            }
        }

        private async Task<string> ExtractArchiveAsync(string src, string dest, CancellationToken ct)
        {
            var ext = Path.GetExtension(src).ToLowerInvariant();
            var outDir = Path.Combine(dest, "extract");
            Directory.CreateDirectory(outDir);

            if (ext == ".zip")
            {
                // Dùng overwrite để tránh lỗi trùng file
                ZipFile.ExtractToDirectory(src, outDir, overwriteFiles: true);
                return outDir;
            }

            // .rar / .7z: dùng 7z.exe
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = _opt.SevenZipPath,
                Arguments = $"x \"{src}\" -o\"{outDir}\" -y",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = System.Diagnostics.Process.Start(psi)!;
            await p.WaitForExitAsync(ct);
            if (p.ExitCode != 0) throw new Exception("7z extract failed: " + await p.StandardError.ReadToEndAsync(ct));
            return outDir;
        }

        private static readonly Regex FileNameRx =
            new(@"^SWD392_PE_SU\d+_SE(?<sid>\d{5,})_(?<name>.+)\.docx$", RegexOptions.IgnoreCase);

        private async Task HandleDocxAsync(AppDbContext db, Job job, string docxPath, CancellationToken ct)
        {
            // === ĐỌC DOCX BẰNG OPEN XML (không cần license) ===
            string text = ReadDocxText(docxPath);

            // Lấy StudentId và tên từ tên file
            var fname = Path.GetFileName(docxPath);
            var m = FileNameRx.Match(fname);
            var fileNameOk = m.Success;
            var sidStr = m.Success ? m.Groups["sid"].Value : null;

            // Map SV
            var student = await db.Students.FirstOrDefaultAsync(s => s.Code.Contains(sidStr ?? ""), ct)
                          ?? new Student { Code = sidStr ?? "UNKNOWN", FullName = m.Success ? m.Groups["name"].Value.Replace('_', ' ') : "Unknown" };
            if (student.Id == Guid.Empty) await db.Students.AddAsync(student, ct);

            // Tạo/đọc Submission
            var sub = await db.Submissions.Include(s => s.Files)
                        .FirstOrDefaultAsync(s => s.AssignmentId == job.AssignmentId && s.StudentId == student.Id, ct);
            if (sub == null)
            {
                sub = new Submission
                {
                    AssignmentId = job.AssignmentId,
                    Student = student,
                    MainDoc = Path.GetRelativePath(_opt.Root, docxPath),
                    Status = SubmissionStatus.Parsed
                };
                await db.Submissions.AddAsync(sub, ct);
            }
            else
            {
                sub.MainDoc = Path.GetRelativePath(_opt.Root, docxPath);
                sub.Status = SubmissionStatus.Parsed;
            }

            // Lưu file record
            sub.Files.Add(new SubmissionFile
            {
                RelPath = Path.GetRelativePath(_opt.Root, docxPath),
                FileName = fname,
                Ext = ".docx",
                IsMainDoc = true,
                TextExcerpt = text.Length > 4000 ? text[..4000] : text
            });

            // Check keyword
            var kwArr = new List<string>();
            if (!string.IsNullOrWhiteSpace(job.Assignment.KeywordsJson))
            {
                try { kwArr = System.Text.Json.JsonSerializer.Deserialize<List<string>>(job.Assignment.KeywordsJson) ?? new List<string>(); }
                catch { }
            }
            var hits = new Dictionary<string, bool>();
            decimal found = 0;
            foreach (var k in kwArr)
            {
                var ok = Regex.IsMatch(text, $@"\b{Regex.Escape(k)}\b", RegexOptions.IgnoreCase);
                hits[k] = ok; if (ok) found++;
            }

            var check = new Check
            {
                Submission = sub,
                FileNameOk = fileNameOk,
                KeywordScore = found,
                KeywordHitsJson = System.Text.Json.JsonSerializer.Serialize(hits),
                Notes = null
            };
            await db.Checks.AddAsync(check, ct);

            // Khởi tạo/cập nhật Score
            var score = await db.Scores.FirstOrDefaultAsync(x => x.SubmissionId == sub.Id, ct);
            if (score == null)
            {
                score = new Score
                {
                    Submission = sub,
                    FileNamePts = fileNameOk ? 1 : 0,
                    KeywordPts = Math.Min(2, found),
                    ManualBonus = 0
                };
                await db.Scores.AddAsync(score, ct);
            }
            else
            {
                score.FileNamePts = fileNameOk ? 1 : 0;
                score.KeywordPts = Math.Min(2, found);
            }

            await db.SaveChangesAsync(ct);
        }

        // ================== NEW: reader .docx ==================
        private static string ReadDocxText(string path)
        {
            var sb = new StringBuilder();
            using var doc = WordprocessingDocument.Open(path, false);

            void AppendParas(OpenXmlElement? container)
            {
                if (container is null) return;
                foreach (var p in container.Descendants<Paragraph>())
                    sb.AppendLine(p.InnerText); // Paragraph.InnerText gộp text trong runs/table cells
            }

            // Nội dung chính
            AppendParas(doc.MainDocumentPart?.Document?.Body);

            // Header/Footer (nếu cần)
            foreach (var h in doc.MainDocumentPart?.HeaderParts ?? Enumerable.Empty<HeaderPart>())
                AppendParas(h.Header);
            foreach (var f in doc.MainDocumentPart?.FooterParts ?? Enumerable.Empty<FooterPart>())
                AppendParas(f.Footer);

            return sb.ToString();
        }
    }
}
