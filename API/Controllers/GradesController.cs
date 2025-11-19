using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Services.Dtos.Requests;
using Services.Dtos.Responses;
using Services.Interfaces;
using Repositories.Entities.Enums;

namespace API.Controllers
{
    [Route("api/grades")]
    [ApiController]
    public class GradesController : ControllerBase
    {
        private readonly IGradingService _gradingService;
        //aaaaa

        public GradesController(IGradingService gradingService)
        {
            _gradingService = gradingService;
        }

        [HttpGet("exams")]
        public async Task<IActionResult> GetAssignedExams([FromQuery] Guid examinerId, CancellationToken ct)
        {
            if (examinerId == Guid.Empty && User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                    ?? User.FindFirst("UserId")?.Value 
                    ?? User.FindFirst("sub")?.Value;
                
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    examinerId = userId;
                }
            }

            if (examinerId == Guid.Empty)
            {
                return BadRequest(new { message = "Examiner ID is required. Provide it as query parameter or ensure you are authenticated." });
            }

            var exams = await _gradingService.GetAssignedExamsAsync(examinerId, ct);
            return Ok(exams);
        }

        [HttpGet("exams/{examId:guid}/submissions")]
        public async Task<IActionResult> GetExamSubmissions([FromRoute] Guid examId, [FromQuery] Guid examinerId, [FromQuery] GetSubmissionsQuery query, CancellationToken ct)
        {
            // Get examiner ID from JWT token claims if not provided in query
            if (examinerId == Guid.Empty && User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                    ?? User.FindFirst("UserId")?.Value 
                    ?? User.FindFirst("sub")?.Value;
                
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    examinerId = userId;
                }
            }

            if (examinerId == Guid.Empty)
            {
                return BadRequest(new { message = "Examiner ID is required. Provide it as query parameter or ensure you are authenticated." });
            }

            var submissions = await _gradingService.GetExamSubmissionsAsync(examId, examinerId, query, ct);
            return Ok(submissions);
        }

        [HttpGet("submissions/{submissionId:guid}/details")]
        public async Task<IActionResult> GetSubmissionDetails([FromRoute] Guid submissionId, [FromQuery] Guid examinerId, CancellationToken ct)
        {
            // Get examiner ID from JWT token claims if not provided in query
            if (examinerId == Guid.Empty && User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                    ?? User.FindFirst("UserId")?.Value 
                    ?? User.FindFirst("sub")?.Value;
                
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    examinerId = userId;
                }
            }

            if (examinerId == Guid.Empty)
            {
                return BadRequest(new { message = "Examiner ID is required. Provide it as query parameter or ensure you are authenticated." });
            }

            var details = await _gradingService.GetSubmissionDetailsAsync(submissionId, examinerId, ct);
            if (details == null)
            {
                return NotFound();
            }
            return Ok(details);
        }

        [HttpGet("exams/{examId:guid}/rubrics")]
        public async Task<IActionResult> GetExamRubrics([FromRoute] Guid examId, CancellationToken ct)
        {
            var rubrics = await _gradingService.GetExamRubricsAsync(examId, ct);
            return Ok(rubrics);
        }

        [HttpPost("submissions/grade")]
        public async Task<IActionResult> SubmitGrades([FromBody] GradingRequests request, CancellationToken ct)
        {
            var examinerId = Guid.Empty;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                    ?? User.FindFirst("UserId")?.Value 
                    ?? User.FindFirst("sub")?.Value;
                
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    examinerId = userId;
                }
            }

            if (examinerId == Guid.Empty)
            {
                return Unauthorized(new { message = "Examiner ID is required. Please authenticate or provide examiner ID." });
            }

            var result = await _gradingService.SubmitGradesAsync(request, examinerId, ct);
            return Ok(result);
        }

        [HttpPut("grades/{gradeId:guid}")]
        public async Task<IActionResult> UpdateGrade([FromRoute] Guid gradeId, [FromBody] UpdateGradeRequest request, CancellationToken ct)
        {
            // Lấy examinerId từ JWT claims
            var examinerId = Guid.Empty;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("UserId")?.Value
                    ?? User.FindFirst("sub")?.Value;

                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    examinerId = userId;
                }
            }

            if (examinerId == Guid.Empty)
            {
                return Unauthorized(new { message = "Examiner ID is required. Please authenticate." });
            }

            var result = await _gradingService.UpdateGradeAsync(gradeId, request, examinerId, ct);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost("submissions/mark-zero")]
        public async Task<IActionResult> MarkZeroDueToViolations([FromBody] MarkZeroRequest request, CancellationToken ct)
        {
            var examinerId = Guid.Empty;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("UserId")?.Value
                    ?? User.FindFirst("sub")?.Value;

                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    examinerId = userId;
                }
            }

            if (examinerId == Guid.Empty)
            {
                return Unauthorized(new { message = "Examiner ID is required. Please authenticate." });
            }

            var result = await _gradingService.MarkZeroDueToViolationsAsync(request, examinerId, ct);

            return Ok(result);
        }

        [HttpGet("submissions/{submissionId:guid}/grades")]
        public async Task<IActionResult> GetSubmissionGrades([FromRoute] Guid submissionId, CancellationToken ct)
        {
            var grades = await _gradingService.GetSubmissionGradesAsync(submissionId, ct);
            return Ok(grades);
        }

        [HttpGet("submissions/{submissionId:guid}/grading-status")]
        public async Task<IActionResult> GetGradingStatus([FromRoute] Guid submissionId, CancellationToken ct)
        {
            var status = await _gradingService.GetGradingStatusAsync(submissionId, ct);
            if (status == null)
            {
                return NotFound();
            }
            return Ok(status);
        }
    }
}


