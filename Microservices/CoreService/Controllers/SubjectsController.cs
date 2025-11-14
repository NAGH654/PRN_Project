using Microsoft.AspNetCore.Mvc;
using CoreService.Services;

namespace CoreService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;
    private readonly ILogger<SubjectsController> _logger;

    public SubjectsController(ISubjectService subjectService, ILogger<SubjectsController> logger)
    {
        _subjectService = subjectService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var subjects = await _subjectService.GetAllAsync();
            return Ok(subjects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all subjects");
            return StatusCode(500, new { message = "An error occurred while retrieving subjects" });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var subject = await _subjectService.GetByIdAsync(id);
            if (subject == null)
                return NotFound(new { message = $"Subject with ID {id} not found" });

            return Ok(subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subject {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the subject" });
        }
    }

    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var subject = await _subjectService.GetByCodeAsync(code);
            if (subject == null)
                return NotFound(new { message = $"Subject with code '{code}' not found" });

            return Ok(subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subject by code {Code}", code);
            return StatusCode(500, new { message = "An error occurred while retrieving the subject" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubjectRequest request)
    {
        try
        {
            var subject = await _subjectService.CreateAsync(
                request.Code,
                request.Name,
                request.Description,
                request.Credits
            );

            return CreatedAtAction(nameof(GetById), new { id = subject.Id }, subject);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating subject");
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating subject");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subject");
            return StatusCode(500, new { message = "An error occurred while creating the subject" });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubjectRequest request)
    {
        try
        {
            var subject = await _subjectService.UpdateAsync(
                id,
                request.Code,
                request.Name,
                request.Description,
                request.Credits
            );

            return Ok(subject);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Subject {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating subject");
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while updating subject");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subject {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating the subject" });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _subjectService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Subject with ID {id} not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subject {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the subject" });
        }
    }
}

public record CreateSubjectRequest(string Code, string Name, string? Description, int Credits);
public record UpdateSubjectRequest(string Code, string Name, string? Description, int Credits);
