using Microsoft.AspNetCore.Mvc;
using Services.Implement;

namespace API.Controllers
{
    [Route("api/scores")]
    [ApiController]
    public class ScoresController(IExportService export) : ControllerBase
    {
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] Guid assignmentId)
        {
            var bytes = await export.ExportScoresExcelAsync(assignmentId);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "scores.xlsx");
        }
    }
}
