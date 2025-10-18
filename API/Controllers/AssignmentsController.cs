using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Services.Models;

namespace API.Controllers
{
    [Route("api/assignments")]
    [ApiController]
    public class AssignmentsController(IAssignmentService svc) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<AssignmentDto>> Create([FromBody] AssignmentCreateDto dto, CancellationToken ct)
        {
            var created = await svc.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<AssignmentDto>>> List(CancellationToken ct)
            => Ok(await svc.ListAsync(ct));

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AssignmentDto>> Get(Guid id, CancellationToken ct)
        {
            var a = await svc.GetAsync(id, ct);
            return a is null ? NotFound() : Ok(a);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] AssignmentUpdateDto dto, CancellationToken ct)
            => await svc.UpdateAsync(id, dto, ct) ? NoContent() : NotFound();

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await svc.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
