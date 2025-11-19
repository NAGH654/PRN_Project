using Microsoft.AspNetCore.Mvc;
using CoreService.Services;

namespace CoreService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SemestersController : ControllerBase
{
    private readonly ISemesterService _semesterService;
    private readonly ILogger<SemestersController> _logger;

    public SemestersController(ISemesterService semesterService, ILogger<SemestersController> logger)
    {
        _semesterService = semesterService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var semesters = await _semesterService.GetAllAsync();
            return Ok(semesters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all semesters");
            return StatusCode(500, new { message = "An error occurred while retrieving semesters" });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var semester = await _semesterService.GetByIdAsync(id);
            if (semester == null)
                return NotFound(new { message = $"Semester with ID {id} not found" });

            return Ok(semester);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting semester {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the semester" });
        }
    }


    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSemesterRequest request)
    {
        try
        {
            var semester = await _semesterService.CreateAsync(
                request.Name,
                request.StartDate,
                request.EndDate
            );

            return CreatedAtAction(nameof(GetById), new { id = semester.Id }, semester);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation while creating semester");
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating semester");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating semester");
            return StatusCode(500, new { message = "An error occurred while creating the semester" });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSemesterRequest request)
    {
        try
        {
            var semester = await _semesterService.UpdateAsync(
                id,
                request.Name,
                request.StartDate,
                request.EndDate
            );

            return Ok(semester);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Semester {Id} not found for update", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation while updating semester {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while updating semester {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating semester {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating the semester" });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _semesterService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Semester with ID {id} not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting semester {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the semester" });
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

public record CreateSemesterRequest(
    string Name,
    DateTime StartDate,
    DateTime EndDate
);

public record UpdateSemesterRequest(
    string Name,
    DateTime StartDate,
    DateTime EndDate
);