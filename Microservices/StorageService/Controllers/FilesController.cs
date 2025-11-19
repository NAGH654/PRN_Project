using Microsoft.AspNetCore.Mvc;
using StorageService.Services;

namespace StorageService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FilesController> _logger;
    private readonly string _storagePath;

    public FilesController(
        IFileService fileService,
        IConfiguration configuration,
        ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _logger = logger;
        _storagePath = configuration["Storage:Path"] ?? "/app/storage";
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var file = await _fileService.GetByIdAsync(id);
            if (file == null)
                return NotFound(new { message = $"File with ID {id} not found" });

            return Ok(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the file" });
        }
    }

    [HttpGet("by-submission/{submissionId:guid}")]
    public async Task<IActionResult> GetBySubmission(Guid submissionId)
    {
        try
        {
            var files = await _fileService.GetBySubmissionIdAsync(submissionId);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files for submission {SubmissionId}", submissionId);
            return StatusCode(500, new { message = "An error occurred while retrieving files" });
        }
    }

    [HttpPost("upload/{submissionId:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(Guid submissionId, IFormFile file)
    {
        try
        {
            var uploadedFile = await _fileService.UploadFileAsync(submissionId, file, _storagePath);
            return CreatedAtAction(nameof(GetById), new { id = uploadedFile.Id }, uploadedFile);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Submission {SubmissionId} not found", submissionId);
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while uploading file");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while uploading file");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for submission {SubmissionId}", submissionId);
            return StatusCode(500, new { message = "An error occurred while uploading the file" });
        }
    }

    [HttpGet("download/{id:guid}")]
    public async Task<IActionResult> Download(Guid id)
    {
        try
        {
            var file = await _fileService.GetByIdAsync(id);
            if (file == null)
                return NotFound(new { message = $"File with ID {id} not found" });

            var filePath = await _fileService.GetFilePathAsync(id);
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            return File(fileBytes, file.FileType ?? "application/octet-stream", file.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "File {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Physical file not found for {Id}", id);
            return NotFound(new { message = "Physical file not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {Id}", id);
            return StatusCode(500, new { message = "An error occurred while downloading the file" });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _fileService.DeleteFileAsync(id);
            if (!deleted)
                return NotFound(new { message = $"File with ID {id} not found" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the file" });
        }
    }
}
