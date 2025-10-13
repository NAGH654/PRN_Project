using Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IAssignmentService
    {
        Task<AssignmentDto> CreateAsync(AssignmentCreateDto dto, CancellationToken ct = default);
        Task<AssignmentDto?> GetAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<AssignmentDto>> ListAsync(CancellationToken ct = default);
        Task<bool> UpdateAsync(Guid id, AssignmentUpdateDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
