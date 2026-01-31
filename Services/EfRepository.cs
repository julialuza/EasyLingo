using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using EasyLingo.Services.Interfaces;

namespace EasyLingo.Services
{
    public class EfRepository<T> : IRepository<T> where T : class
    {
        private readonly DbContext _db;
        private readonly DbSet<T> _set;

        public EfRepository(DbContext db)
        {
            _db = db;
            _set = db.Set<T>();
        }

        public IQueryable<T> Query() => _set;

        public async Task<T?> GetByIdAsync(params object[] keyValues)
            => await _set.FindAsync(keyValues);

        public async Task AddAsync(T entity)
        {
            _set.Add(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(T entity)
        {
            _set.Update(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(T entity)
        {
            _set.Remove(entity);
            await _db.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<T> items)
        {
            await _db.AddRangeAsync(items);
        }

        public Task<int> SaveChangesAsync()
        {
            return _db.SaveChangesAsync();
        }

    }
}
