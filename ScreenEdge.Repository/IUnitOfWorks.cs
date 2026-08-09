using ScreenEdge.Entity.Entities;

namespace ScreenEdge.Repository;

public interface IUnitOfWorks : IDisposable
{
    IBaseRepository<TickerHistory> TickerHistoryRepository { get; }
    IBaseRepository<DistinctStock> DistinctStockRepository { get; }
    IBaseRepository<Screener> ScreenerRepository { get; }
    Task<int> CompleteAsync();
}
