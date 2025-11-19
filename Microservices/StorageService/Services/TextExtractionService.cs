using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using StorageService.Data;
using StorageService.Models;

namespace StorageService.Services;

public class TextExtractionService : ITextExtractionService
{
    private readonly StorageDbContext _context;
    private readonly ILogger<TextExtractionService> _logger;
    private readonly string _storagePath;

    public TextExtractionService(
        StorageDbContext context,
        ILogger<TextExtractionService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _storagePath = configuration["Storage:BasePath"] ?? Path.Combine(AppContext.BaseDirectory, "storage");
    }

    public async Task<SubmissionTextResponse?> GetSubmissionTextAsync(Guid submissionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var submission = await _context.Submissions
                .Include(s => s.Files)
                .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);

            if (submission == null)
            {
                _logger.LogWarning("Submission not found: {SubmissionId}", submissionId);
                return null;
            }

            // Find the first .docx file
            var docxFile = submission.Files.FirstOrDefault(f => f.FileType?.ToLower() == "docx" || f.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase));
            if (docxFile == null)
            {
                _logger.LogWarning("No DOCX file found for submission: {SubmissionId}", submissionId);
                return new SubmissionTextResponse
                {
                    SubmissionId = submissionId,
                    Text = string.Empty,
                    WordCount = 0,
                    CharCount = 0
                };
            }

            var fullPath = Path.Combine(_storagePath, docxFile.FilePath);
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found: {FilePath}", fullPath);
                return new SubmissionTextResponse
                {
                    SubmissionId = submissionId,
                    Text = "File not found",
                    WordCount = 0,
                    CharCount = 0
                };
            }

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from file: {FilePath}", fullPath);
                return new SubmissionTextResponse
                {
                    SubmissionId = submissionId,
                    Text = $"Error extracting text: {ex.Message}",
                    WordCount = 0,
                    CharCount = 0
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing text extraction for submission: {SubmissionId}", submissionId);
            throw;
        }
    }

    private static string ExtractTextFromDocx(string path)
    {
        using var doc = WordprocessingDocument.Open(path, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;

        var sb = new StringBuilder();

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
        // Collapse multiple new lines
        s = Regex.Replace(s, "\n{3,}", "\n\n");
        // Collapse excessive spaces
        s = Regex.Replace(s, @"[\t ]{2,}", " ");
        return s.Trim();
    }

    private static int CountWords(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return 0;
        return Regex.Matches(input, @"\b\w+\b").Count;
    }
}
