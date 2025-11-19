using CoreService.Entities;
using CoreService.DTOs;

namespace CoreService.Services;

public interface IExamSessionService
{
    Task<ExamSessionDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ExamSessionDto>> GetAllAsync();
    Task<IEnumerable<ExamSessionDto>> GetActiveAsync();
    Task<IEnumerable<ExamSessionDto>> GetByExamIdAsync(Guid examId);
    Task<ExamSessionDto> CreateAsync(CreateExamSessionRequest request);
    Task<ExamSessionDto> UpdateAsync(Guid id, UpdateExamSessionRequest request);
    Task DeleteAsync(Guid id);
}
