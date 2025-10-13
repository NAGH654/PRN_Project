using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repo
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly AppDbContext _db;
        protected readonly DbSet<T> _set;
        public GenericRepository(AppDbContext db) { _db = db; _set = db.Set<T>(); }
        public async Task<T> AddAsync(T e) { await _set.AddAsync(e); return e; }
        public Task<T?> GetAsync(Guid id) => _set.FindAsync(id).AsTask();
        public async Task<IEnumerable<T>> FindAsync(System.Linq.Expressions.Expression<Func<T, bool>> p)
          => await _set.Where(p).ToListAsync();
        public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
