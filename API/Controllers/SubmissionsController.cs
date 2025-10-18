using API.Adapters;
using API.Request;
using Microsoft.AspNetCore.Mvc;
using Services.Service;

namespace API.Controllers
{
    [Route("api/submissions")]
    [ApiController]
    public class SubmissionsController(ISubmissionService svc) : ControllerBase
    {
        [HttpPost("batch")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadBatch([FromForm] UploadBatchForm form, CancellationToken ct)
        {
            var result = await svc.UploadBatchAsync(
                new UploadBatchRequest(form.AssignmentId, new FormFileUpload(form.Archive), null));
            return Ok(result);
        }
    }
}
