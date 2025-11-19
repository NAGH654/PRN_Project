using Microsoft.EntityFrameworkCore;
using StorageService.Data;
using StorageService.Entities;

namespace StorageService.Repositories;

public class FileRepository : IFileRepository
{
    private readonly StorageDbContext _context;

    public FileRepository(StorageDbContext context)
    {
        _context = context;
    }

    public async Task<SubmissionFile?> GetByIdAsync(Guid id)
    {
        return await _context.SubmissionFiles
            .Include(sf => sf.Submission)
            .FirstOrDefaultAsync(sf => sf.Id == id);
    }

    public async Task<IEnumerable<SubmissionFile>> GetBySubmissionIdAsync(Guid submissionId)
    {
        return await _context.SubmissionFiles
            .Where(sf => sf.SubmissionId == submissionId)
            .OrderBy(sf => sf.FileName)
            .ToListAsync();
    }

    public async Task<SubmissionFile?> GetByHashAsync(string fileHash)
    {
        return await _context.SubmissionFiles
            .FirstOrDefaultAsync(sf => sf.FileHash == fileHash);
    }

    public async Task<SubmissionFile> CreateAsync(SubmissionFile file)
    {
        file.Id = Guid.NewGuid();
        file.UploadedAt = DateTime.UtcNow;
        
        _context.SubmissionFiles.Add(file);
        await _context.SaveChangesAsync();
        
        return file;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var file = await _context.SubmissionFiles.FindAsync(id);
        if (file == null)
            return false;

        _context.SubmissionFiles.Remove(file);
        await _context.SaveChangesAsync();
        
        return true;
    }
}
