using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using Services.Interfaces;
using Services.Models;

namespace Services.Implement
{
    public class AssignmentService(AppDbContext db) : IAssignmentService
    {
        public async Task<AssignmentDto> CreateAsync(AssignmentCreateDto dto, CancellationToken ct = default)
        {
            // kiểm tra duy nhất (ClassId + Code)
            var exists = await db.Assignments.AnyAsync(a => a.ClassId == dto.ClassId && a.Code == dto.Code, ct);
            if (exists) throw new InvalidOperationException("Assignment code already exists in this class.");

            var entity = new Assignment
            {
                ClassId = dto.ClassId,
                Code = dto.Code,
                Name = dto.Name,
                NamingRegex = dto.NamingRegex,
                KeywordsJson = dto.KeywordsJson,
                RubricJson = dto.RubricJson,
                DueAt = dto.DueAt
            };
            db.Assignments.Add(entity);
            await db.SaveChangesAsync(ct);

            return ToDto(entity);
        }

        public async Task<AssignmentDto?> GetAsync(Guid id, CancellationToken ct = default)
        {
            var e = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return e is null ? null : ToDto(e);
        }

        public async Task<IReadOnlyList<AssignmentDto>> ListAsync(CancellationToken ct = default)
        {
            return await db.Assignments.AsNoTracking()
                .Select(a => new AssignmentDto
                {
                    Id = a.Id,
                    ClassId = a.ClassId,
                    Code = a.Code,
                    Name = a.Name,
                    NamingRegex = a.NamingRegex,
                    KeywordsJson = a.KeywordsJson,
                    RubricJson = a.RubricJson,
                    DueAt = a.DueAt
                }).ToListAsync(ct);
        }

        public async Task<bool> UpdateAsync(Guid id, AssignmentUpdateDto dto, CancellationToken ct = default)
        {
            var e = await db.Assignments.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return false;

            if (dto.Name is not null) e.Name = dto.Name;
            if (dto.NamingRegex is not null) e.NamingRegex = dto.NamingRegex;
            if (dto.KeywordsJson is not null) e.KeywordsJson = dto.KeywordsJson;
            if (dto.RubricJson is not null) e.RubricJson = dto.RubricJson;
            if (dto.DueAt.HasValue) e.DueAt = dto.DueAt;

            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var e = await db.Assignments.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return false;
            db.Assignments.Remove(e);
            await db.SaveChangesAsync(ct);
            return true;
        }

        private static AssignmentDto ToDto(Assignment a) => new()
        {
            Id = a.Id,
            ClassId = a.ClassId,
            Code = a.Code,
            Name = a.Name,
            NamingRegex = a.NamingRegex,
            KeywordsJson = a.KeywordsJson,
            RubricJson = a.RubricJson,
            DueAt = a.DueAt
        };
    }
}
