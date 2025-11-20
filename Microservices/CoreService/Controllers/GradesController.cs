 using Microsoft.AspNetCore.Mvc;
 using CoreService.Services;
 using CoreService.DTOs;

namespace CoreService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GradesController : ControllerBase
{
    private readonly IGradeService _gradeService;
    private readonly ILogger<GradesController> _logger;

    public GradesController(IGradeService gradeService, ILogger<GradesController> logger)
    {
        _gradeService = gradeService;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var grade = await _gradeService.GetByIdAsync(id);
            if (grade == null)
                return NotFound(new { message = $"Grade with ID {id} not found" });

            return Ok(grade);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grade {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the grade" });
        }
    }

    [HttpGet("by-exam/{examId:guid}")]
    public async Task<IActionResult> GetByExam(Guid examId)
    {
        try
        {
            var grades = await _gradeService.GetByExamIdAsync(examId);
            return Ok(grades);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grades for exam {ExamId}", examId);
            return StatusCode(500, new { message = "An error occurred while retrieving grades" });
        }
    }

    [HttpGet("by-student/{studentId:guid}")]
    public async Task<IActionResult> GetByStudent(Guid studentId)
    {
        try
        {
            var grades = await _gradeService.GetByStudentIdAsync(studentId);
            return Ok(grades);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grades for student {StudentId}", studentId);
            return StatusCode(500, new { message = "An error occurred while retrieving grades" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrUpdate([FromBody] CreateGradeRequest request)
    {
        try
        {
            var grade = await _gradeService.CreateOrUpdateGradeAsync(
                request.ExamId,
                request.StudentId,
                request.Score,
                request.Feedback,
                request.GradedBy
            );

            return Ok(grade);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found while creating/updating grade");
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating/updating grade");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating grade");
            return StatusCode(500, new { message = "An error occurred while saving the grade" });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _gradeService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Grade with ID {id} not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting grade {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the grade" });
        }
    }

    // New rubric-based grading endpoints (Use Case 3)
    [HttpPost("grade-submission")]
    public async Task<IActionResult> GradeSubmission([FromBody] GradingRequest request)
    {
        try
        {
            var response = await _gradeService.GradeSubmissionAsync(request);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found while grading submission");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while grading submission");
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while grading submission");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error grading submission");
            return StatusCode(500, new { message = "An error occurred while grading the submission" });
        }
    }

    [HttpGet("submission/{submissionId:guid}")]
    public async Task<IActionResult> GetSubmissionGrades(Guid submissionId)
    {
        try
        {
            var response = await _gradeService.GetSubmissionGradesAsync(submissionId);
            if (response == null)
                return NotFound(new { message = $"No grades found for submission {submissionId}" });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grades for submission {SubmissionId}", submissionId);
            return StatusCode(500, new { message = "An error occurred while retrieving grades" });
        }
    }

    [HttpGet("exams/{examId:guid}")]
    public async Task<IActionResult> GetExamSubmissions(Guid examId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            // Get current user ID from JWT token (simplified - would extract from claims)
            var examinerId = Guid.NewGuid(); // TODO: Extract from JWT token

            var response = await _gradeService.GetExamSubmissionsAsync(examId, examinerId, pageNumber, pageSize);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found while getting exam submissions");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submissions for exam {ExamId}", examId);
            return StatusCode(500, new { message = "An error occurred while retrieving exam submissions" });
        }
    }

    [HttpGet("examiner/{examinerId:guid}/assigned")]
    public async Task<IActionResult> GetExaminerAssignedSubmissions(Guid examinerId)
    {
        try
        {
            var submissions = await _gradeService.GetExaminerAssignedSubmissionsAsync(examinerId);
            return Ok(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assigned submissions for examiner {ExaminerId}", examinerId);
            return StatusCode(500, new { message = "An error occurred while retrieving assigned submissions" });
        }
    }

    [HttpPost("submission/{submissionId:guid}/finalize")]
    public async Task<IActionResult> FinalizeGrades(Guid submissionId, [FromBody] FinalizeGradesRequest request)
    {
        try
        {
            var success = await _gradeService.FinalizeGradesAsync(submissionId, request.ModeratorId);
            if (!success)
                return NotFound(new { message = $"Submission {submissionId} not found or no grades to finalize" });

            return Ok(new { message = "Grades finalized successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing grades for submission {SubmissionId}", submissionId);
            return StatusCode(500, new { message = "An error occurred while finalizing grades" });
        }
    }

    [HttpGet("requiring-review")]
    public async Task<IActionResult> GetSubmissionsRequiringReview()
    {
        try
        {
            var submissions = await _gradeService.GetSubmissionsRequiringReviewAsync();
            return Ok(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submissions requiring review");
            return StatusCode(500, new { message = "An error occurred while retrieving submissions" });
        }
    }

    // Additional grading APIs (migrated from monolithic)
    [HttpGet("examiner/{examinerId:guid}/assigned-exams")]
    public async Task<IActionResult> GetAssignedExams(Guid examinerId)
    {
        try
        {
            var exams = await _gradeService.GetAssignedExamsAsync(examinerId);
            return Ok(exams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assigned exams for examiner {ExaminerId}", examinerId);
            return StatusCode(500, new { message = "An error occurred while retrieving assigned exams" });
        }
    }

    [HttpGet("submission/{submissionId:guid}/details")]
    public async Task<IActionResult> GetSubmissionDetails(Guid submissionId)
    {
        try
        {
            // Get current user ID from JWT token (simplified)
            var examinerId = Guid.NewGuid(); // TODO: Extract from JWT token

            var response = await _gradeService.GetSubmissionDetailsAsync(submissionId, examinerId);
            if (response == null)
                return NotFound(new { message = $"Submission {submissionId} not found or access denied" });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission details for {SubmissionId}", submissionId);
            return StatusCode(500, new { message = "An error occurred while retrieving submission details" });
        }
    }

    [HttpGet("exam/{examId:guid}/rubrics")]
    public async Task<IActionResult> GetExamRubrics(Guid examId)
    {
        try
        {
            var rubrics = await _gradeService.GetExamRubricsAsync(examId);
            return Ok(rubrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rubrics for exam {ExamId}", examId);
            return StatusCode(500, new { message = "An error occurred while retrieving exam rubrics" });
        }
    }

    [HttpPut("grade/{gradeId:guid}")]
    public async Task<IActionResult> UpdateGrade(Guid gradeId, [FromBody] UpdateGradeRequest request)
    {
        try
        {
            // Get current user ID from JWT token (simplified)
            var examinerId = Guid.NewGuid(); // TODO: Extract from JWT token

            var response = await _gradeService.UpdateGradeAsync(gradeId, request, examinerId);
            if (response == null)
                return NotFound(new { message = $"Grade {gradeId} not found" });

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Grade not found while updating");
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access while updating grade");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating grade");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating grade {GradeId}", gradeId);
            return StatusCode(500, new { message = "An error occurred while updating the grade" });
        }
    }

    [HttpPost("submission/{submissionId:guid}/mark-zero")]
    public async Task<IActionResult> MarkZeroDueToViolations(Guid submissionId, [FromBody] MarkZeroRequest request)
    {
        try
        {
            // Get current user ID from JWT token (simplified)
            var examinerId = Guid.NewGuid(); // TODO: Extract from JWT token

            var response = await _gradeService.MarkZeroDueToViolationsAsync(submissionId, request, examinerId);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Submission not found while marking zero");
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access while marking zero");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while marking zero");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking submission {SubmissionId} as zero", submissionId);
            return StatusCode(500, new { message = "An error occurred while marking submission as zero" });
        }
    }

    [HttpGet("submission/{submissionId:guid}/status")]
    public async Task<IActionResult> GetGradingStatus(Guid submissionId)
    {
        try
        {
            var response = await _gradeService.GetGradingStatusAsync(submissionId);
            if (response == null)
                return NotFound(new { message = $"Submission {submissionId} not found" });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grading status for submission {SubmissionId}", submissionId);
            return StatusCode(500, new { message = "An error occurred while retrieving grading status" });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "CoreService",
            version = "1.0.0",
            architecture = "3-layer",
            timestamp = DateTime.UtcNow
        });
    }
}

public record CreateGradeRequest(
    Guid ExamId,
    Guid StudentId,
    decimal Score,
    string? Feedback,
    Guid GradedBy
);

public record FinalizeGradesRequest(
    Guid ModeratorId
);
