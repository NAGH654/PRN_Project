using Microsoft.AspNetCore.Mvc;
using CoreService.Services;

namespace CoreService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExamsController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly ILogger<ExamsController> _logger;

    public ExamsController(IExamService examService, ILogger<ExamsController> logger)
    {
        _examService = examService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var exams = await _examService.GetAllAsync();
            return Ok(exams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all exams");
            return StatusCode(500, new { message = "An error occurred while retrieving exams" });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var exam = await _examService.GetByIdAsync(id);
            if (exam == null)
                return NotFound(new { message = $"Exam with ID {id} not found" });

            return Ok(exam);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exam {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the exam" });
        }
    }

    [HttpGet("by-subject/{subjectId:guid}")]
    public async Task<IActionResult> GetBySubject(Guid subjectId)
    {
        try
        {
            var exams = await _examService.GetBySubjectIdAsync(subjectId);
            return Ok(exams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exams for subject {SubjectId}", subjectId);
            return StatusCode(500, new { message = "An error occurred while retrieving exams" });
        }
    }

    [HttpGet("by-semester/{semesterId:guid}")]
    public async Task<IActionResult> GetBySemester(Guid semesterId)
    {
        try
        {
            var exams = await _examService.GetBySemesterIdAsync(semesterId);
            return Ok(exams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exams for semester {SemesterId}", semesterId);
            return StatusCode(500, new { message = "An error occurred while retrieving exams" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExamRequest request)
    {
        try
        {
            var exam = await _examService.CreateAsync(
                request.Title,
                request.Description,
                request.SubjectId,
                request.SemesterId,
                request.ExamDate,
                request.DurationMinutes,
                request.TotalMarks
            );

            return CreatedAtAction(nameof(GetById), new { id = exam.Id }, exam);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found while creating exam");
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating exam");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating exam");
            return StatusCode(500, new { message = "An error occurred while creating the exam" });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExamRequest request)
    {
        try
        {
            var exam = await _examService.UpdateAsync(
                id,
                request.Title,
                request.Description,
                request.SubjectId,
                request.SemesterId,
                request.ExamDate,
                request.DurationMinutes,
                request.TotalMarks
            );

            return Ok(exam);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found while updating exam {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while updating exam");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating exam {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating the exam" });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _examService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Exam with ID {id} not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting exam {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the exam" });
        }
    }
}

public record CreateExamRequest(
    string Title,
    string? Description,
    Guid SubjectId,
    Guid SemesterId,
    DateTime ExamDate,
    int DurationMinutes,
    decimal TotalMarks
);

public record UpdateExamRequest(
    string Title,
    string? Description,
    Guid SubjectId,
    Guid SemesterId,
    DateTime ExamDate,
    int DurationMinutes,
    decimal TotalMarks
);
