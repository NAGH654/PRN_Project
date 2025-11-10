using Microsoft.AspNetCore.Mvc;
using Services.Dtos.Requests;
using Services.Interfaces;
using Services.Dtos.Responses;

namespace API.Controllers
{
    [Route("api/submissions")]
    [ApiController]
    public class SubmissionsController : ControllerBase
    {
        private readonly ISubmissionProcessingService _service;
        private readonly ISubmissionQueryService _queryService;

        public SubmissionsController(ISubmissionProcessingService service, ISubmissionQueryService queryService)
        {
            _service = service;
            _queryService = queryService;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(600L * 1024L * 1024L)]
        public async Task<IActionResult> Upload([FromForm] UploadBatchForm form, CancellationToken ct)
        {
            var result = await _service.ProcessArchiveAsync(form, ct);
            return Ok(result);
        }

        [HttpPost("upload/nested-zip")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(600L * 1024L * 1024L)] // 600 MB (must match Kestrel MaxRequestBodySize)
        public async Task<IActionResult> UploadNestedZip([FromForm] UploadBatchForm form, CancellationToken ct)
        {
            try
            {
                if (form?.Archive == null)
                {
                    return BadRequest("Archive file is required.");
                }

                var result = await _service.ProcessNestedZipArchiveAsync(form, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, detail = ex.ToString() });
            }
        }

        [HttpGet("{submissionId:guid}/images")]
        public async Task<IActionResult> GetImages([FromRoute] Guid submissionId, CancellationToken ct)
        {
            var list = await _queryService.GetSubmissionImagesAsync(submissionId, ct);
            // Build absolute URLs
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var data = list.Select(x => new
            {
                x.ImageId,
                x.ImageName,
                url = $"{baseUrl}/files/{x.RelativePath}",
                x.ImageSize
            });
            return Ok(data);
        }

        [HttpGet("session/{sessionId:guid}/students")]
        public async Task<IActionResult> GetSessionStudents([FromRoute] Guid sessionId, CancellationToken ct)
        {
            var list = await _queryService.GetSessionStudentsAsync(sessionId, ct);
            return Ok(list);
        }

        [HttpGet("{submissionId:guid}/text")]
        public async Task<IActionResult> GetSubmissionText([FromRoute] Guid submissionId, CancellationToken ct)
        {
            var res = await _queryService.GetSubmissionTextAsync(submissionId, ct);
            if (res == null) return NotFound();
            return Ok(res);
        }
    }
}
