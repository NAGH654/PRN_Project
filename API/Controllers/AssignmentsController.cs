using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using Services.Interfaces;
using Services.Models;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssignmentsController : ControllerBase
    {
        private readonly IAssignmentService _svc;
        public AssignmentsController(IAssignmentService svc) => _svc = svc;

        [HttpPost]
        public async Task<ActionResult<AssignmentDto>> Create([FromBody] AssignmentCreateDto dto, CancellationToken ct)
        {
            var created = await _svc.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AssignmentDto>>> List(CancellationToken ct)
            => Ok(await _svc.ListAsync(ct));

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AssignmentDto>> Get(Guid id, CancellationToken ct)
        {
            var a = await _svc.GetAsync(id, ct);
            return a is null ? NotFound() : Ok(a);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] AssignmentUpdateDto dto, CancellationToken ct)
            => await _svc.UpdateAsync(id, dto, ct) ? NoContent() : NotFound();

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _svc.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
