---
name: data-models-and-entities
description: Skill for the data models, database entities, enums, and repository pattern used by the Technical Screener system. Covers PriceHistory, Screener entity, TickerHistory, DistinctStock, StrategyEnum, and the Repository/UnitOfWork pattern implementation.
---

# Data Models & Entities Skill

## Overview
This skill covers all data models, database entities, enums, and the repository pattern that the Technical Screener system needs. This is a subset of the original MarketWeave data layer — only the screener-relevant parts.

## Required Entities

### TickerHistory (Database Entity)
Stores historical OHLCV data per stock symbol. This is the primary data source for the screener.
```csharp
public class TickerHistory
{
    public long Id { get; set; }
    public string Symbol { get; set; }
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}
```

### DistinctStock (Database Entity)
Master list of unique stock symbols available for screening.
```csharp
public class DistinctStock
{
    public long Id { get; set; }
    public string Symbol { get; set; }
    public string CompanyName { get; set; }
    public string Exchange { get; set; }  // "NSE" or "BSE"
}
```

### Screener (Database Entity)
Stores screener results/signals detected by the engine.
```csharp
public class Screener
{
    public long Id { get; set; }
    public string Symbol { get; set; }
    public string ScreenerName { get; set; }     // StrategyEnum value
    public string TimeFrame { get; set; }         // "D" (Daily) or "W" (Weekly)
    public DateTime RecognizeDate { get; set; }   // Date signal was detected
    public double Rsi { get; set; }               // RSI (Daily) value at signal
    public double RsiWeekly { get; set; }         // RSI on weekly timeframe
    public double RsiMonthly { get; set; }        // RSI on monthly timeframe
    public long Volume { get; set; }              // Volume at signal
    public double RecognizedPrice { get; set; }   // Close price at signal
}
```

### PriceHistory (Indicator Model)
The in-memory model used by all indicator calculations. Lives in `Ta.Indicator.Base`.
```csharp
public class PriceHistory
{
    public DateTime Date { get; set; }
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public double Volume { get; set; }
}
```

**Important:** `TickerHistory` uses `decimal` for prices; `PriceHistory` uses `double`. The conversion happens in the ScreenerEngine when fetching data:
```csharp
List<PriceHistory> dailyData = context.TickerHistories
    .Where(w => w.Symbol == symbol)
    .Select(h => new PriceHistory
    {
        Date = h.Date,
        Open = (double)h.Open,
        High = (double)h.High,
        Low = (double)h.Low,
        Close = (double)h.Close,
        Volume = (double)h.Volume
    })
    .OrderBy(h => h.Date)
    .ToList();
```

## Enums

### StrategyEnum
```csharp
public enum StrategyEnum
{
    NOLAG,                // ZeroLag screener
    EMAFIFTY,             // EMA-50 crossover screener
    SUPPORTRESISTANCE,    // Support/Resistance breakout screener
    RSIWMA                // RSI-Weighted Moving Average screener
}
```

## Repository Pattern

### IBaseRepository<T>
```csharp
public interface IBaseRepository<T> where T : class
{
    Task<T> GetByIdAsync(long id, params Expression<Func<T, object>>[] includeProperties);
    Task<List<T>> GetAllAsync(params Expression<Func<T, object>>[] includeProperties);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity);
    Task DeleteAsync(long id);
    void RemoveRange(IEnumerable<T> entities);
}
```

### IUnitOfWorks (Screener-relevant subset)
```csharp
public interface IUnitOfWorks : IDisposable
{
    Task CompleteAsync();
    IBaseRepository<TickerHistory> TickerHistoryRepository { get; }
    IBaseRepository<DistinctStock> DistinctStockRepository { get; }
    IBaseRepository<Screener> ScreenerRepository { get; }
}
```

### DI Registration
```csharp
builder.Services.AddScoped<IUnitOfWorks, UnitOfWorks>();
```

## Database Configuration
- **Provider:** SQL Server (LocalDB)
- **Connection string:** `(localdb)\\MSSQLLocalDB`, database name TBD for new project
- **All entities** use `long Id` as primary key
- **EF Core 9** with `Microsoft.EntityFrameworkCore.SqlServer 9.0.9`

## Key Design Decisions
1. Keep `PriceHistory` (double) separate from `TickerHistory` (decimal) — indicators need fast floating-point math
2. `Screener` entity stores the strategy name as a string (not FK) for flexibility
3. Repository pattern provides abstraction over EF Core for testability
