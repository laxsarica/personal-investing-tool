using ScreenEdge.Entity;
using ScreenEdge.Entity.Entities;

namespace ScreenEdge.Repository;

public class UnitOfWorks : IUnitOfWorks
{
    private readonly AppDbContext _context;
    private IBaseRepository<TickerHistory>? _tickerHistoryRepository;
    private IBaseRepository<DistinctStock>? _distinctStockRepository;
    private IBaseRepository<Screener>? _screenerRepository;

    public UnitOfWorks(AppDbContext context)
    {
        _context = context;
    }

    public IBaseRepository<TickerHistory> TickerHistoryRepository =>
        _tickerHistoryRepository ??= new BaseRepository<TickerHistory>(_context);

    public IBaseRepository<DistinctStock> DistinctStockRepository =>
        _distinctStockRepository ??= new BaseRepository<DistinctStock>(_context);

    public IBaseRepository<Screener> ScreenerRepository =>
        _screenerRepository ??= new BaseRepository<Screener>(_context);

    public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
