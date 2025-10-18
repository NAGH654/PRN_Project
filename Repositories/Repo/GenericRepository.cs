using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Interfaces;

namespace Repositories.Repo
{
  public class GenericRepository<T>(AppDbContext db) : IGenericRepository<T> where T : class
  {
    protected readonly DbSet<T> _set = db.Set<T>();
    public async Task<T?> GetAsync(Guid id) => await _set.FindAsync(id);
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> p)
    {
      return await _set.Where(p).ToListAsync();
    }

    public Task<int> SaveChangesAsync() => db.SaveChangesAsync();
  }
}
