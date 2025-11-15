using Microsoft.AspNetCore.Mvc;
using StorageService.Services;

namespace StorageService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissionService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly ILogger<SubmissionsController> _logger;

    public SubmissionsController(
        ISubmissionService submissionService,
        ITextExtractionService textExtractionService,
        ILogger<SubmissionsController> logger)
    {
        _submissionService = submissionService;
        _textExtractionService = textExtractionService;
        _logger = logger;
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

    [HttpGet("by-student/{studentId:guid}")]
    public async Task<IActionResult> GetByStudent(Guid studentId)
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

public record CreateSubmissionRequest(Guid StudentId, Guid ExamId, Guid ExamSessionId);
public record UpdateStatusRequest(string Status, string? Notes);
