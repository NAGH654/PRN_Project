using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Interfaces;

namespace Repositories.Repo
{
  public class GenericRepository<T>(AppDbContext db) : IGenericRepository<T> where T : class, IEntity
  {
    protected readonly DbSet<T> _set = db.Set<T>();
    public async Task<T?> GetAsync(Guid id)
    {
      var entity = await _set.FindAsync(id);
      if (entity != null)
      {
        _set.Entry(entity).State = EntityState.Detached;
      }
      return entity;
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> p)
    {
      return await _set.Where(p).ToListAsync();
    }

    public virtual async Task AddAsync(T entity)
    {
      entity.Id = Guid.NewGuid();
      _set.Add(entity);
      await db.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(T entity)
    {
      var tracker = _set.Attach(entity);
      tracker.State = EntityState.Modified;
      await db.SaveChangesAsync();
    }

    public virtual async Task RemoveAsync(T entity)
    {
      _set.Remove(entity);
      await db.SaveChangesAsync();
    }

    #region TRANSACTION SUPPORT

    // Deferred operations for UnitOfWork
    public virtual void AddDeferred(T entity) => _set.AddAsync(entity);

    public virtual void UpdateDeferred(T entity)
    {
      var tracker = _set.Attach(entity);
      tracker.State = EntityState.Modified;
    }

    public virtual void RemoveDeferred(T entity) => _set.Remove(entity);

    #endregion

    public Task<int> SaveChangesAsync() => db.SaveChangesAsync();
  }
}
