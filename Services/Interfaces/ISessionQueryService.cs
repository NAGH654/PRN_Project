using Services.Dtos.Responses;

namespace Services.Interfaces
{
    public interface ISessionQueryService
    {
        Task<List<ExamSessionResponse>> GetAllAsync(CancellationToken ct = default);
        Task<List<ExamSessionResponse>> GetActiveAsync(CancellationToken ct = default);
    }
}


