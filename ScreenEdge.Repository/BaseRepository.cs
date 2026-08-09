using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ScreenEdge.Entity;

namespace ScreenEdge.Repository;

public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public BaseRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(long id, params Expression<Func<T, object>>[] includeProperties)
    {
        IQueryable<T> query = _dbSet;
        foreach (var include in includeProperties)
            query = query.Include(include);
        return await query.FirstOrDefaultAsync(e => EF.Property<long>(e, "Id") == id);
    }

    public async Task<List<T>> GetAllAsync(params Expression<Func<T, object>>[] includeProperties)
    {
        IQueryable<T> query = _dbSet;
        foreach (var include in includeProperties)
            query = query.Include(include);
        return await query.ToListAsync();
    }

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public async Task AddRangeAsync(IEnumerable<T> entities) => await _dbSet.AddRangeAsync(entities);

    public Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity != null)
            _dbSet.Remove(entity);
    }

    public void RemoveRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);

    public IQueryable<T> Query() => _dbSet.AsQueryable();
}
