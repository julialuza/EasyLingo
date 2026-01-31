using System.Linq;
using System.Threading.Tasks;

namespace EasyLingo.Services.Interfaces
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> Query();
        Task<T?> GetByIdAsync(params object[] keyValues);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> items);
        Task<int> SaveChangesAsync();

    }
}
