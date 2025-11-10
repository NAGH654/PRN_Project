using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Repositories.Data;
using Services.Dtos.Responses;
using Services.Interfaces;
using Services.Dtos;

namespace Services.Implement
{
    public class SubmissionQueryService : ISubmissionQueryService
    {
        private readonly AppDbContext _db;
        private readonly StorageOptions _storageOptions;

        public SubmissionQueryService(AppDbContext db, IOptions<StorageOptions> storage)
        {
            _db = db;
            _storageOptions = storage.Value;
        }

        public async Task<List<SubmissionImageResponse>> GetSubmissionImagesAsync(Guid submissionId, CancellationToken ct = default)
        {
            return await _db.SubmissionImages
                .AsNoTracking()
                .Where(x => x.SubmissionId == submissionId)
                .OrderBy(x => x.ImageName)
                .Select(x => new SubmissionImageResponse
                {
                    ImageId = x.ImageId,
                    ImageName = x.ImageName,
                    RelativePath = x.ImagePath.Replace('\\', '/'),
                    ImageSize = x.ImageSize
                })
                .ToListAsync(ct);
        }

        public async Task<List<SubmissionStudentItem>> GetSessionStudentsAsync(Guid sessionId, CancellationToken ct = default)
        {
            return await _db.Submissions
                .AsNoTracking()
                .Where(s => s.SessionId == sessionId)
                .OrderBy(s => s.StudentId)
                .ThenBy(s => s.StudentName)
                .Select(s => new SubmissionStudentItem
                {
                    SubmissionId = s.SubmissionId,
                    StudentId = s.StudentId,
                    StudentName = s.StudentName,
                    FileName = s.FileName,
                    SubmissionTime = s.SubmissionTime
                })
                .ToListAsync(ct);
        }

        public async Task<SubmissionTextResponse?> GetSubmissionTextAsync(Guid submissionId, CancellationToken ct = default)
        {
            var sub = await _db.Submissions.AsNoTracking().FirstOrDefaultAsync(s => s.SubmissionId == submissionId, ct);
            if (sub == null) return null;

            var storageRoot = Path.Combine(AppContext.BaseDirectory, _storageOptions.Root);
            var fullPath = Path.Combine(storageRoot, sub.FilePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath)) return new SubmissionTextResponse
            {
                SubmissionId = submissionId,
                Text = string.Empty,
                WordCount = 0,
                CharCount = 0
            };

            if (!string.Equals(Path.GetExtension(fullPath), ".docx", StringComparison.OrdinalIgnoreCase))
            {
                return new SubmissionTextResponse
                {
                    SubmissionId = submissionId,
                    Text = string.Empty,
                    WordCount = 0,
                    CharCount = 0
                };
            }

            string text = ExtractTextFromDocx(fullPath);
            text = NormalizeText(text);
            return new SubmissionTextResponse
            {
                SubmissionId = submissionId,
                Text = text,
                WordCount = CountWords(text),
                CharCount = text.Length
            };
        }

        private static string ExtractTextFromDocx(string path)
        {
            using var doc = WordprocessingDocument.Open(path, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return string.Empty;

            var sb = new System.Text.StringBuilder();

            foreach (var para in body.Descendants<Paragraph>())
            {
                foreach (var txt in para.Descendants<Text>())
                {
                    sb.Append(txt.Text);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string NormalizeText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.Replace('\r', '\n');
            // collapse multiple new lines
            s = System.Text.RegularExpressions.Regex.Replace(s, "\n{3,}", "\n\n");
            // collapse excessive spaces
            s = System.Text.RegularExpressions.Regex.Replace(s, "[\t ]{2,}", " ");
            return s.Trim();
        }

        private static int CountWords(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0;
            return System.Text.RegularExpressions.Regex.Matches(input, @"\b\w+\b").Count;
        }
    }
}


