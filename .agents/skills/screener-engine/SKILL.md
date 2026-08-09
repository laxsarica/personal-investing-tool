---
name: screener-engine
description: Skill for the Stock Screener Engine — the orchestrator that runs all technical screeners (ZeroLag, EmaFifty, SupportResistance, RsiWMA) across all NSE equity stocks in parallel. Covers screener logic, filtering criteria, signal detection rules, and the database persistence pattern. This is the core automation module.
---

# Screener Engine Skill

## Overview
The ScreenerEngine is the central orchestration class that scans all NSE equity stocks against multiple technical screening strategies. It runs screeners in parallel (up to 10 concurrent), collects signals, filters by RSI range, and persists results to the database.

## Source Location
Original: `MarketWeave.Screener/ScreenerEngine.cs` (248 lines)

## Dependencies
The ScreenerEngine depends on:
- `Ta.Indicator` — for RSI calculation
- `Ta.CustomIndicator` — for ZeroLag, EmaFifty, SupportResistance, RsiWMA indicators
- `MarketWeave.Entity` — for `AppDbContext`, `Screener` entity, `PriceHistory` (mapped from `TickerHistory`)
- `MarketWeave.Repository` — for `IUnitOfWorks`
- `MarketWeave.Broker` — for `InstrumentJsonModel`, `GetMasterData` (exchange master data)
- `MarketWeave.Models` — for `StrategyEnum`

## Architecture

### Constructor
```csharp
public class ScreenerEngine(IUnitOfWorks uow)
```
Uses primary constructor with Unit of Work injection.

### Entry Point
```csharp
public async Task<object> RunScreenerJob()
```

### Execution Flow
1. **Load stock universe** — `GetMasterData.GetAllNseEquity()` returns all NSE equity instruments
2. **Parallel scan** — `Parallel.ForEach` with `MaxDegreeOfParallelism = 10`
3. **Per stock:**
   - Fetch daily OHLCV from `TickerHistories` table, ordered by date
   - Skip if < 30 bars
   - Convert to weekly OHLC using `DataConverter.ConvertToWeeklyOHLC()`
   - Calculate RSI (14-period) for daily and weekly
   - Run 4 screeners: EmaFifty, ZeroLag, SupportResistance, RsiWMA
4. **Persist** — Filter nulls, bulk insert all `Screener` results via `AppDbContext`
5. **Return** — Anonymous object with `Time` (minutes) and `Record` (count)

### Screener Methods

#### 1. ZeroLagScreener
```csharp
private List<Screener> ZeroLagScreener(string symbol, List<PriceHistory> daily, List<PriceHistory> weekly)
```
- **Weekly scan:** If RSI > 55 and previous bar was NOT UpSignal but current bar IS UpSignal → signal
- **Daily scan:** Same crossover logic
- **Filter:** RSI between 55 and 70
- **Strategy enum:** `StrategyEnum.NOLAG`

#### 2. EmaFiftyScreener
```csharp
private List<Screener> EmaFiftyScreener(string symbol, List<PriceHistory> daily, List<PriceHistory> weekly)
```
- **Weekly scan:** If RSI > 55 and EmaFifty returns UpSignal → signal
- **Daily scan:** Same logic
- **Filter:** RSI between 55 and 70
- **Strategy enum:** `StrategyEnum.EMAFIFTY`

#### 3. SupportResistanceScreener
```csharp
private List<Screener> SupportResistanceScreener(string symbol, List<PriceHistory> daily)
```
- **Daily only:** Needs > 20 bars
- **Condition:** RSI > 54, last signal is UpSignal, signal date within last 2 days
- **Strategy enum:** `StrategyEnum.SUPPORTRESISTANCE`

#### 4. RsiWeightedMovingAverageScreener
```csharp
private List<Screener> RsiWeightedMovingAverageScreener(string symbol, List<PriceHistory> daily)
```
- **Daily only:** Needs > 55 bars
- **Condition:** Last result has `BullishCross == true` AND RSI > 55
- **Filter:** RSI between 55 and 70
- **Strategy enum:** `StrategyEnum.RSIWMA`

### Screener Result Entity
```csharp
public class Screener
{
    public string Symbol { get; set; }
    public string ScreenerName { get; set; }     // Strategy enum name
    public string TimeFrame { get; set; }         // "D" or "W"
    public DateTime RecognizeDate { get; set; }   // Signal date
    public double Rsi { get; set; }               // RSI at signal
    public long Volume { get; set; }              // Volume at signal
    public double RecognizedPrice { get; set; }   // Close price at signal
}
```

### Signal Filtering Rules
All screeners apply an **RSI band filter**: results are only kept if `55 <= RSI <= 70`. This targets stocks in a healthy uptrend (not overbought, not oversold).

### Known Issues to Fix During Migration
1. **Thread safety:** `screeners.AddRange()` inside `Parallel.ForEach` is NOT thread-safe — use `ConcurrentBag<Screener>` instead
2. **Silent exception swallowing:** ZeroLag, SupportResistance, and RsiWMA screeners catch and ignore all exceptions
3. **Direct DbContext creation:** Uses `new AppDbContext()` inside parallel loop instead of proper DI
4. **Hardcoded parallelism:** `MaxDegreeOfParallelism = 10` should be configurable
5. **Missing RsiDaily/RsiWeekly encapsulation:** Instance-level fields are mutated inside parallel loop (race condition)

## Automation Considerations
This module is designed to be run as an automated scheduled job:
- Can be triggered via API endpoint (`/api/screener/run`)
- Execution time depends on stock count (typically 2000+ NSE equities)
- Results should be timestamped and stored for historical comparison
- Consider adding a background job framework (Hangfire, Quartz.NET) for scheduling
