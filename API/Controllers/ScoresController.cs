using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Service;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScoresController : ControllerBase
    {
        private readonly IExportService _export;
        public ScoresController(IExportService export) { _export = export; }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] Guid assignmentId)
        {
            var bytes = await _export.ExportScoresExcelAsync(assignmentId);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "scores.xlsx");
        }
    }
}
