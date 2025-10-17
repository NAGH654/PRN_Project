using API.Adapters;
using API.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Service;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubmissionsController : ControllerBase
    {
        private readonly ISubmissionService _svc;
        public SubmissionsController(ISubmissionService svc) => _svc = svc;

        [HttpPost("batch")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadBatch([FromForm] UploadBatchForm form, CancellationToken ct)
        {
            var result = await _svc.UploadBatchAsync(
                new UploadBatchRequest(form.AssignmentId, new FormFileUpload(form.Archive), null));
            return Ok(result);
        }
    }
}
