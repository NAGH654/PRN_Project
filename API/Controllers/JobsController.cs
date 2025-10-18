using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace API.Controllers
{
    [Route("api/jobs")]
    [ApiController]
    public class JobsController(IJobService jobs) : ControllerBase
    {
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var job = await jobs.GetAsync(id, ct);
            return job is null ? NotFound() : Ok(job);
        }
    }
}
