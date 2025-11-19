using Microsoft.AspNetCore.Mvc;
using CoreService.Services;
using CoreService.DTOs;

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
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation while creating exam");
            return BadRequest(new { message = ex.Message });
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

    // Rubric Management
    [HttpPost("{examId:guid}/rubrics")]
    public async Task<IActionResult> AddRubricItem(Guid examId, [FromBody] AddRubricItemRequest request)
    {
        try
        {
            var rubricItem = await _examService.AddRubricItemAsync(
                examId,
                request.Criteria,
                request.Description,
                request.MaxPoints
            );

            return CreatedAtAction(nameof(GetById), new { id = examId }, rubricItem);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found while adding rubric item to exam {ExamId}", examId);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while adding rubric item to exam {ExamId}", examId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding rubric item to exam {ExamId}", examId);
            return StatusCode(500, new { message = "An error occurred while adding the rubric item" });
        }
    }

    [HttpDelete("{examId:guid}/rubrics/{rubricItemId:guid}")]
    public async Task<IActionResult> RemoveRubricItem(Guid examId, Guid rubricItemId)
    {
        try
        {
            var removed = await _examService.RemoveRubricItemAsync(examId, rubricItemId);
            if (!removed)
                return NotFound(new { message = "Rubric item not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while removing rubric item from exam {ExamId}", examId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing rubric item {RubricItemId} from exam {ExamId}", rubricItemId, examId);
            return StatusCode(500, new { message = "An error occurred while removing the rubric item" });
        }
    }

    // Publishing
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> PublishExam(Guid id)
    {
        try
        {
            var exam = await _examService.PublishExamAsync(id);
            return Ok(exam);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Exam {Id} not found for publishing", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while publishing exam {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing exam {Id}", id);
            return StatusCode(500, new { message = "An error occurred while publishing the exam" });
        }
    }

    // Examiner Assignment
    [HttpPost("sessions/{sessionId:guid}/examiners")]
    public async Task<IActionResult> AssignExaminer(Guid sessionId, [FromBody] AssignExaminerRequest request)
    {
        try
        {
            var assignment = await _examService.AssignExaminerAsync(sessionId, request.ExaminerId, request.Role);
            return CreatedAtAction(nameof(GetById), new { id = sessionId }, assignment);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found while assigning examiner to session {SessionId}", sessionId);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while assigning examiner to session {SessionId}", sessionId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning examiner to session {SessionId}", sessionId);
            return StatusCode(500, new { message = "An error occurred while assigning the examiner" });
        }
    }

    [HttpDelete("sessions/{sessionId:guid}/examiners/{examinerId:guid}")]
    public async Task<IActionResult> RemoveExaminerAssignment(Guid sessionId, Guid examinerId)
    {
        try
        {
            var removed = await _examService.RemoveExaminerAssignmentAsync(sessionId, examinerId);
            if (!removed)
                return NotFound(new { message = "Examiner assignment not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing examiner {ExaminerId} from session {SessionId}", examinerId, sessionId);
            return StatusCode(500, new { message = "An error occurred while removing the examiner assignment" });
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

public record AddRubricItemRequest(
    string Criteria,
    string? Description,
    decimal MaxPoints
);

public record AssignExaminerRequest(
    Guid ExaminerId,
    string Role = "Examiner"
);
