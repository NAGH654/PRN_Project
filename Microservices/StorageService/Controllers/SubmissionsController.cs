using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageService.Services;
using StorageService.Models;
using StorageService.DTOs;
using StorageService.Data;

namespace StorageService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissionService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly INestedZipService _nestedZipService;
    private readonly StorageDbContext _context;
    private readonly ILogger<SubmissionsController> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public SubmissionsController(
        ISubmissionService submissionService,
        ITextExtractionService textExtractionService,
        INestedZipService nestedZipService,
        StorageDbContext context,
        ILogger<SubmissionsController> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _submissionService = submissionService;
        _textExtractionService = textExtractionService;
        _nestedZipService = nestedZipService;
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var submission = await _submissionService.GetByIdAsync(id);
            if (submission == null)
                return NotFound(new { message = $"Submission with ID {id} not found" });

            return Ok(submission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the submission" });
        }
    }

    [HttpGet("by-student/{studentId}")]
    public async Task<IActionResult> GetByStudent(string studentId)
    {
        try
        {
            var submissions = await _submissionService.GetByStudentIdAsync(studentId);
            return Ok(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submissions for student {StudentId}", studentId);
            return StatusCode(500, new { message = "An error occurred while retrieving submissions" });
        }
    }

    [HttpGet("by-exam/{examId:guid}")]
    public async Task<IActionResult> GetByExam(Guid examId)
    {
        try
        {
            var submissions = await _submissionService.GetByExamIdAsync(examId);
            return Ok(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submissions for exam {ExamId}", examId);
            return StatusCode(500, new { message = "An error occurred while retrieving submissions" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubmissionRequest request)
    {
        try
        {
            var submission = await _submissionService.CreateSubmissionAsync(
                request.StudentId,
                request.ExamId,
                request.ExamSessionId
            );

            return CreatedAtAction(nameof(GetById), new { id = submission.Id }, submission);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating submission");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating submission");
            return StatusCode(500, new { message = "An error occurred while creating the submission" });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            var submission = await _submissionService.UpdateSubmissionStatusAsync(
                id,
                request.Status,
                request.Notes
            );

            return Ok(submission);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Submission {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while updating status");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating submission status {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating the submission" });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _submissionService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Submission with ID {id} not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting submission {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the submission" });
        }
    }

    [HttpGet("{id:guid}/text")]
    public async Task<IActionResult> GetSubmissionText(Guid id)
    {
        try
        {
            var result = await _textExtractionService.GetSubmissionTextAsync(id);
            if (result == null)
                return NotFound(new { message = $"Submission with ID {id} not found" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from submission {Id}", id);
            return StatusCode(500, new { message = "An error occurred while extracting text" });
        }
    }

    [HttpGet("by-session/{sessionId:guid}")]
    public async Task<IActionResult> GetBySession(Guid sessionId)
    {
        try
        {
            var submissions = await _submissionService.GetBySessionIdAsync(sessionId);
            return Ok(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submissions for session {SessionId}", sessionId);
            return StatusCode(500, new { message = "An error occurred while retrieving submissions" });
        }
    }

    [HttpGet("session/{sessionId:guid}/students")]
    public async Task<IActionResult> GetSessionStudents(Guid sessionId)
    {
        try
        {
            var submissions = await _submissionService.GetBySessionIdAsync(sessionId);
            var students = submissions.Select(s => new
            {
                SubmissionId = s.Id,
                StudentId = s.StudentId,
                StudentName = (string?)null, // TODO: Get from User table if needed
                FileName = $"submission_{s.Id}.zip", // TODO: Get actual filename if stored
                SubmissionTime = s.SubmittedAt
            }).ToList();
            return Ok(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting students for session {SessionId}", sessionId);
            return StatusCode(500, new { message = "An error occurred while retrieving session students" });
        }
    }

    [HttpDelete("cleanup/all")]
    public async Task<IActionResult> DeleteAllSubmissions(CancellationToken ct)
    {
        try
        {
            // Delete in correct order to avoid foreign key violations
            var imagesCount = await _context.SubmissionImages.CountAsync(ct);
            var violationsCount = await _context.Violations.CountAsync(ct);
            var filesCount = await _context.SubmissionFiles.CountAsync(ct);
            var submissionsCount = await _context.Submissions.CountAsync(ct);

            _context.SubmissionImages.RemoveRange(_context.SubmissionImages);
            _context.Violations.RemoveRange(_context.Violations);
            _context.SubmissionFiles.RemoveRange(_context.SubmissionFiles);
            _context.Submissions.RemoveRange(_context.Submissions);

            await _context.SaveChangesAsync(ct);

            return Ok(new
            {
                message = "All submissions deleted successfully",
                deleted = new
                {
                    submissions = submissionsCount,
                    files = filesCount,
                    images = imagesCount,
                    violations = violationsCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all submissions");
            return StatusCode(500, new { message = "An error occurred while deleting submissions", detail = ex.Message });
        }
    }

    [HttpGet("{id:guid}/images")]
    public async Task<IActionResult> GetImages(Guid id, CancellationToken ct)
    {
        try
        {
            var submission = await _submissionService.GetByIdAsync(id);
            if (submission == null)
                return NotFound(new { message = $"Submission with ID {id} not found" });

            // Get images from database
            var images = await _context.SubmissionImages
                .Where(img => img.SubmissionId == id)
                .Select(img => new
                {
                    ImageId = img.Id,
                    ImageName = img.ImageName,
                    RelativePath = img.ImagePath,
                    ImageSize = img.ImageSizeBytes
                })
                .ToListAsync(ct);

            // Build absolute URLs through Gateway
            // Check for X-Forwarded-Host header (set by Gateway)
            var forwardedHost = Request.Headers["X-Forwarded-Host"].FirstOrDefault();
            var forwardedProto = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? "http";
            
            string baseUrl;
            if (!string.IsNullOrEmpty(forwardedHost))
            {
                // Use Gateway host from forwarded header
                baseUrl = $"{forwardedProto}://{forwardedHost}";
            }
            else
            {
                // Fallback: try to get Gateway URL from configuration
                var gatewayUrl = _configuration["Gateway:BaseUrl"] 
                    ?? _configuration["Gateway__BaseUrl"]
                    ?? "http://localhost:5000";
                baseUrl = gatewayUrl;
            }

            // Build URL through Gateway route: /api/files/{relativePath}
            var data = images.Select(x => new
            {
                x.ImageId,
                x.ImageName,
                url = $"{baseUrl}/api/files/{x.RelativePath}",
                x.ImageSize
            });

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting images for submission {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving images" });
        }
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(600L * 1024L * 1024L)]
    public async Task<IActionResult> Upload([FromForm] UploadBatchForm form, CancellationToken ct)
    {
        // Validate session exists by calling CoreService
        var coreServiceUrl = _configuration["Services:CoreService"];
        var sessionResponse = await _httpClient.GetAsync($"{coreServiceUrl}/api/sessions/active", ct);
        if (!sessionResponse.IsSuccessStatusCode)
        {
            return BadRequest("No active exam sessions available");
        }

        // TODO: Implement full file processing logic migrated from monolithic API
        // For now, return a placeholder response
        var result = new ProcessingResult
        {
            JobId = Guid.NewGuid().ToString(),
            TotalFiles = 0,
            SubmissionsCreated = 0,
            ViolationsCreated = 0,
            ImagesExtracted = 0
        };

        return Ok(result);
    }

    [HttpPost("upload/nested-zip")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(600L * 1024L * 1024L)] // 600 MB (must match Kestrel MaxRequestBodySize)
    public async Task<IActionResult> UploadNestedZip([FromForm] UploadBatchForm form, CancellationToken ct)
    {
        try
        {
            if (form?.Archive == null)
            {
                return BadRequest(new { message = "Archive file is required." });
            }

            var result = await _nestedZipService.ProcessNestedZipArchiveAsync(form, ct);
            _logger.LogInformation("Upload completed. CreatedSubmissions count: {Count}, Total: {Total}, Processed: {Processed}", 
                result.CreatedSubmissions?.Count ?? 0, result.TotalFiles, result.ProcessedFiles);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for nested ZIP upload");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Processing failed for nested ZIP upload");
            return StatusCode(500, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during nested ZIP upload");
            return StatusCode(500, new { message = "An unexpected error occurred", detail = ex.Message });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "StorageService",
            version = "1.0.0",
            architecture = "3-layer",
            timestamp = DateTime.UtcNow
        });
    }
}

public record CreateSubmissionRequest(string StudentId, Guid ExamId, Guid ExamSessionId);
public record UpdateStatusRequest(string Status, string? Notes);
