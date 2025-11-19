using StorageService.Entities;
using StorageService.DTOs;

namespace StorageService.Services;

public interface ISubmissionService
{
    Task<SubmissionDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<SubmissionDto>> GetByStudentIdAsync(string studentId);
    Task<IEnumerable<SubmissionDto>> GetByExamIdAsync(Guid examId);
    Task<IEnumerable<SubmissionDto>> GetBySessionIdAsync(Guid sessionId);
    Task<SubmissionDto> CreateSubmissionAsync(string studentId, Guid examId, Guid examSessionId);
    Task<SubmissionDto> UpdateSubmissionStatusAsync(Guid id, string status, string? notes = null);
    Task<bool> DeleteAsync(Guid id);
}
