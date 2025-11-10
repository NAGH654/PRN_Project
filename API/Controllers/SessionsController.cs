using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace API.Controllers
{
    [Route("api/sessions")]
    [ApiController]
    public class SessionsController : ControllerBase
    {
        private readonly ISessionQueryService _sessions;

        public SessionsController(ISessionQueryService sessions)
        {
            _sessions = sessions;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            return Ok(await _sessions.GetAllAsync(ct));
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive(CancellationToken ct)
        {
            return Ok(await _sessions.GetActiveAsync(ct));
        }
    }
}


