using CoreService.Entities;
using CoreService.Services;
using CoreService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly IExamSessionService _service;

    public SessionsController(IExamSessionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExamSessionDto>>> GetAll()
    {
        var sessions = await _service.GetAllAsync();
        return Ok(sessions);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<ExamSessionDto>>> GetActive()
    {
        var sessions = await _service.GetActiveAsync();
        return Ok(sessions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExamSessionDto>> GetById(Guid id)
    {
        var session = await _service.GetByIdAsync(id);
        if (session == null)
            return NotFound();

        return Ok(session);
    }

    [HttpGet("by-exam/{examId}")]
    public async Task<ActionResult<IEnumerable<ExamSessionDto>>> GetByExamId(Guid examId)
    {
        var sessions = await _service.GetByExamIdAsync(examId);
        return Ok(sessions);
    }

    [HttpPost]
    public async Task<ActionResult<ExamSessionDto>> Create([FromBody] CreateExamSessionRequest request)
    {
        try
        {
            var created = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExamSessionDto>> Update(Guid id, [FromBody] UpdateExamSessionRequest request)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, request);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
