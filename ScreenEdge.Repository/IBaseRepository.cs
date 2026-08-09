using System.Linq.Expressions;

namespace ScreenEdge.Repository;

public interface IBaseRepository<T> where T : class
{
    Task<T?> GetByIdAsync(long id, params Expression<Func<T, object>>[] includeProperties);
    Task<List<T>> GetAllAsync(params Expression<Func<T, object>>[] includeProperties);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity);
    Task DeleteAsync(long id);
    void RemoveRange(IEnumerable<T> entities);
    IQueryable<T> Query();
}
