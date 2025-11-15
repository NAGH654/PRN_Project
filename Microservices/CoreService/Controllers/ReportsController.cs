using CoreService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoreService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet("exams")]
    public async Task<IActionResult> GetExamReport(
        [FromQuery] Guid? subjectId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var report = await _reportService.GetExamReportAsync(subjectId, fromDate, toDate);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating exam report");
            return StatusCode(500, new { message = "An error occurred while generating the report" });
        }
    }
}
