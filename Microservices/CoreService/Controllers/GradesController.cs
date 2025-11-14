using Microsoft.AspNetCore.Mvc;
using CoreService.Services;

namespace CoreService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GradesController : ControllerBase
{
    private readonly IGradeService _gradeService;
    private readonly ILogger<GradesController> _logger;

    public GradesController(IGradeService gradeService, ILogger<GradesController> logger)
    {
        _gradeService = gradeService;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var grade = await _gradeService.GetByIdAsync(id);
            if (grade == null)
                return NotFound(new { message = $"Grade with ID {id} not found" });

            return Ok(grade);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grade {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the grade" });
        }
    }

    [HttpGet("by-exam/{examId:guid}")]
    public async Task<IActionResult> GetByExam(Guid examId)
    {
        try
        {
            var grades = await _gradeService.GetByExamIdAsync(examId);
            return Ok(grades);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grades for exam {ExamId}", examId);
            return StatusCode(500, new { message = "An error occurred while retrieving grades" });
        }
    }

    [HttpGet("by-student/{studentId:guid}")]
    public async Task<IActionResult> GetByStudent(Guid studentId)
    {
        try
        {
            var grades = await _gradeService.GetByStudentIdAsync(studentId);
            return Ok(grades);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grades for student {StudentId}", studentId);
            return StatusCode(500, new { message = "An error occurred while retrieving grades" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate([FromBody] CreateGradeRequest request)
    {
        try
        {
            var grade = await _gradeService.CreateOrUpdateGradeAsync(
                request.ExamId,
                request.StudentId,
                request.Score,
                request.Feedback,
                request.GradedBy
            );

            return Ok(grade);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found while creating/updating grade");
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating/updating grade");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating grade");
            return StatusCode(500, new { message = "An error occurred while saving the grade" });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _gradeService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Grade with ID {id} not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting grade {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the grade" });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "CoreService",
            version = "1.0.0",
            architecture = "3-layer",
            timestamp = DateTime.UtcNow
        });
    }
}

public record CreateGradeRequest(
    Guid ExamId,
    Guid StudentId,
    decimal Score,
    string? Feedback,
    Guid GradedBy
);
