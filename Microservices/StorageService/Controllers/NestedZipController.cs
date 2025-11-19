using Microsoft.AspNetCore.Mvc;
using StorageService.Models;
using StorageService.Services;

namespace StorageService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NestedZipController : ControllerBase
{
    private readonly INestedZipService _service;
    private readonly ILogger<NestedZipController> _logger;

    public NestedZipController(INestedZipService service, ILogger<NestedZipController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(600L * 1024L * 1024L)] // 600 MB
    public async Task<IActionResult> UploadNestedZip([FromForm] UploadBatchForm form, CancellationToken ct)
    {
        try
        {
            if (form?.Archive == null)
            {
                return BadRequest(new { message = "Archive file is required." });
            }

            var result = await _service.ProcessNestedZipArchiveAsync(form, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Processing failed");
            return StatusCode(500, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during nested ZIP processing");
            return StatusCode(500, new { message = "An unexpected error occurred", detail = ex.Message });
        }
    }
}
