using CoreService.Entities;
using CoreService.Services;
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
    public async Task<ActionResult<IEnumerable<ExamSession>>> GetAll()
    {
        var sessions = await _service.GetAllAsync();
        return Ok(sessions);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<ExamSession>>> GetActive()
    {
        var sessions = await _service.GetActiveAsync();
        return Ok(sessions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExamSession>> GetById(Guid id)
    {
        var session = await _service.GetByIdAsync(id);
        if (session == null)
            return NotFound();

        return Ok(session);
    }

    [HttpGet("by-exam/{examId}")]
    public async Task<ActionResult<IEnumerable<ExamSession>>> GetByExamId(Guid examId)
    {
        var sessions = await _service.GetByExamIdAsync(examId);
        return Ok(sessions);
    }

    [HttpPost]
    public async Task<ActionResult<ExamSession>> Create([FromBody] ExamSession session)
    {
        try
        {
            var created = await _service.CreateAsync(session);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExamSession>> Update(Guid id, [FromBody] ExamSession session)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, session);
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
