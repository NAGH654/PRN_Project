using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Services.Interfaces;

namespace API.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reports;

        public ReportsController(IReportService reports)
        {
            _reports = reports;
        }

        [HttpGet("submissions")]
        public async Task<IActionResult> GetSubmissions(
            [FromQuery] Guid? examId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            var (total, rows) = await _reports.GetSubmissionsAsync(examId, from, to, page, pageSize, ct);
            return Ok(new { total, page, pageSize, data = rows });
        }

        [HttpGet("submissions/export")]
        public async Task<IActionResult> ExportSubmissions(
            [FromQuery] Guid? examId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken ct = default)
        {
            var fileBytes = await _reports.ExportSubmissionsAsync(examId, from, to, ct);
            var fileName = $"SubmissionsReport_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("submissions/odata")]
        [EnableQuery(PageSize = 100)]
        public IActionResult GetSubmissionsOData()
        {
            var queryable = _reports.GetSubmissionsODataQueryable();
            return Ok(queryable);
        }
    }
}


