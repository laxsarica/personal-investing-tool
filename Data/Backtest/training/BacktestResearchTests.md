# BacktestResearchTests — Original Source Reference

> **Source:** `ScreenEdge.Tests/BacktestResearchTests.cs`  
> **Purpose:** Training data reference for ML/LLM model development  
> **Preserved:** This file contains the original xUnit-based backtest implementation before extraction into the standalone `ScreenEdge.Backtest` project.

## Strategy: GFS RSI (RSITTF) — Grandfather-Father-Son

### Core Logic
- **Grandfather (Monthly RSI)** > 60 — confirms long-term uptrend
- **Father (Weekly RSI)** > 60 — confirms medium-term uptrend  
- **Son (Daily RSI)** near 40 — pullback zone within uptrend (buying the dip)
- All three must align simultaneously

### Evaluation Criteria
- **Win:** 10-day forward return ≥ 5.0%
- **Loss:** 10-day forward return ≤ -3.0%
- **Neutral:** Between -3.0% and +5.0%

### Forward Return Windows
- 5-day, 10-day, 20-day, 40-day returns calculated
- Max drawdown (worst intra-period low) tracked
- Max gain (best intra-period high) tracked

### Grid Search Parameters (Original)
- Monthly RSI thresholds: [50, 55, 60, 65, 70]
- Weekly RSI thresholds: [50, 55, 60, 65, 70]
- Pullback zones: [30, 35, 40] (with 10-point band)
- RSI periods: [5, 7, 14] *(later fixed to 14 only)*
- Minimum 3 signals required per parameter set

---

## Original Source Code

```csharp
using Microsoft.EntityFrameworkCore;
using ScreenEdge.Entity;
using ScreenEdge.Entity.Entities;
using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;
using TA.Indicators.Indicator;
using Xunit.Abstractions;
using ScreenerEntity = ScreenEdge.Entity.Entities.Screener;

namespace ScreenEdge.Tests;

/// <summary>
/// Research-grade backtest tests that connect to the real ScreenEdgeDb database.
/// These are NOT unit tests — they are analytical tools to evaluate screener strategy performance.
/// Run selectively with: dotnet test --filter "FullyQualifiedName~BacktestResearch"
/// </summary>
public class BacktestResearchTests
{
    private readonly ITestOutputHelper _output;

    // Backtest configuration
    private const double WinThresholdPercent = 5.0;
    private const double LossThresholdPercent = -3.0;
    private const string ConnectionString = "Server=localhost;Database=ScreenEdgeDb;Trusted_Connection=True;TrustServerCertificate=True;";

    public BacktestResearchTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Backtest ALL screener strategies — calculates forward returns for every historical signal
    /// and produces a strategy leaderboard ranked by win rate.
    /// </summary>
    [Fact]
    public async Task Backtest_AllStrategies_ProduceLeaderboard()
    {
        using var context = CreateDbContext();

        var signals = await context.Screeners
            .OrderBy(s => s.RecognizeDate)
            .ToListAsync();

        _output.WriteLine($"Total signals to backtest: {signals.Count}");
        _output.WriteLine(new string('=', 100));

        var results = new List<BacktestRow>();

        foreach (var signal in signals)
        {
            var row = await BacktestSignal(context, signal);
            if (row != null)
                results.Add(row);
        }

        _output.WriteLine($"\nBacktested {results.Count} signals (skipped {signals.Count - results.Count} with insufficient forward data)\n");

        // Strategy Leaderboard
        PrintLeaderboard(results);

        // Per-strategy breakdown
        foreach (var strategy in results.Select(r => r.StrategyName).Distinct().OrderBy(s => s))
        {
            PrintStrategyDetail(strategy, results.Where(r => r.StrategyName == strategy).ToList());
        }

        Assert.True(results.Count > 0, "No backtest results generated — check if Screeners table has data");
    }

    /// <summary>
    /// Backtest only RSITTF (GFS RSI) strategy with detailed per-signal output.
    /// </summary>
    [Fact]
    public async Task Backtest_RsiTtf_DetailedAnalysis()
    {
        using var context = CreateDbContext();

        var signals = await context.Screeners
            .Where(s => s.ScreenerName == "RSITTF")
            .OrderBy(s => s.RecognizeDate)
            .ToListAsync();

        _output.WriteLine($"RSITTF signals to backtest: {signals.Count}");
        _output.WriteLine(new string('=', 100));

        var results = new List<BacktestRow>();

        foreach (var signal in signals)
        {
            var row = await BacktestSignal(context, signal);
            if (row != null)
            {
                results.Add(row);
                _output.WriteLine(
                    $"{row.Symbol,-15} {row.SignalDate:yyyy-MM-dd}  Entry:{row.EntryPrice,10:F2}  " +
                    $"5D:{row.Return5D,7:F2}%  10D:{row.Return10D,7:F2}%  20D:{row.Return20D,7:F2}%  " +
                    $"MaxDD:{row.MaxDrawdown,7:F2}%  MaxGain:{row.MaxGain,7:F2}%  [{row.Outcome}]");
            }
        }

        if (results.Count > 0)
        {
            _output.WriteLine(new string('-', 100));
            PrintStrategyDetail("RSITTF", results);
        }

        Assert.True(true); // Research test — always passes
    }

    /// <summary>
    /// Grid-search RSITTF thresholds to find optimal Monthly/Weekly RSI levels.
    /// Tests all combinations and ranks by win rate.
    /// </summary>
    [Fact]
    public async Task Optimize_RsiTtf_GridSearch()
    {
        using var context = CreateDbContext();

        // Load all daily data grouped by symbol (only symbols with enough history)
        var symbols = await context.DistinctStocks
            .Where(s => s.Exchange == "NSE" && s.TotalTradingDays >= 365)
            .Select(s => s.Symbol)
            .ToListAsync();

        _output.WriteLine($"Testing {symbols.Count} symbols with 365+ trading days");
        _output.WriteLine(new string('=', 100));

        // Grid search parameters
        double[] monthlyThresholds = [50, 55, 60, 65, 70];
        double[] weeklyThresholds = [50, 55, 60, 65, 70];
        double[] pullbackLows = [30, 35, 40];
        int[] rsiPeriods = [5, 7, 14];

        var optimizationResults = new List<OptimizationRow>();

        foreach (double monthlyTh in monthlyThresholds)
        foreach (double weeklyTh in weeklyThresholds)
        foreach (double pullbackLow in pullbackLows)
        foreach (int rsiPeriod in rsiPeriods)
        {
            double pullbackHigh = pullbackLow + 10; // 10-point band

            var signals = new List<(string Symbol, DateTime Date, double EntryPrice, double Rsi)>();

            foreach (var symbol in symbols)
            {
                var dailyData = await context.TickerHistories
                    .Where(w => w.Symbol == symbol)
                    .OrderBy(h => h.Date)
                    .Select(h => new PriceHistory
                    {
                        Date = h.Date,
                        Open = (double)h.Open,
                        High = (double)h.High,
                        Low = (double)h.Low,
                        Close = (double)h.Close,
                        Volume = (double)h.Volume
                    })
                    .ToListAsync();

                if (dailyData.Count <= 365)
                    continue;

                var weeklyOhlc = DataConverter.ConvertToWeeklyOHLC(dailyData);
                var monthlyOhlc = DataConverter.ConvertToMonthlyOHLC(dailyData);

                double rsiMonthly = GetRsi(monthlyOhlc, 14);
                double rsiWeekly = GetRsi(weeklyOhlc, 14);

                if (rsiMonthly <= monthlyTh || rsiWeekly <= weeklyTh)
                    continue;

                // Son: daily RSI crossover through pullback zone
                var rsiCalc = new RSI(rsiPeriod) { PriceHistoryList = dailyData };
                var rsiValues = rsiCalc.Calculate().ResultData.TakeLast(2).ToList();

                if (rsiValues.Count == 2 &&
                    rsiValues[0].Value < pullbackLow &&
                    rsiValues[1].Value > pullbackLow &&
                    rsiValues[1].Value <= pullbackHigh)
                {
                    signals.Add((symbol, dailyData.Last().Date, dailyData.Last().Close, rsiValues[1].Value ?? 0));
                }
            }

            if (signals.Count == 0)
                continue;

            // Backtest these signals
            int wins = 0, losses = 0, neutral = 0;
            double totalReturn = 0;

            foreach (var sig in signals)
            {
                var futurePrices = await context.TickerHistories
                    .Where(t => t.Symbol == sig.Symbol && t.Date > sig.Date)
                    .OrderBy(t => t.Date)
                    .Take(15)
                    .Select(t => (double)t.Close)
                    .ToListAsync();

                if (futurePrices.Count < 10)
                    continue;

                double ret10D = (futurePrices[9] - sig.EntryPrice) / sig.EntryPrice * 100;
                totalReturn += ret10D;

                if (ret10D >= WinThresholdPercent) wins++;
                else if (ret10D <= LossThresholdPercent) losses++;
                else neutral++;
            }

            int total = wins + losses + neutral;
            if (total < 3) continue; // Need at least 3 signals

            optimizationResults.Add(new OptimizationRow
            {
                MonthlyThreshold = monthlyTh,
                WeeklyThreshold = weeklyTh,
                PullbackLow = pullbackLow,
                PullbackHigh = pullbackHigh,
                RsiPeriod = rsiPeriod,
                TotalSignals = total,
                Wins = wins,
                Losses = losses,
                WinRate = (double)wins / total * 100,
                AvgReturn = totalReturn / total
            });
        }

        // Sort by win rate, then by signal count
        var ranked = optimizationResults
            .OrderByDescending(r => r.WinRate)
            .ThenByDescending(r => r.TotalSignals)
            .Take(20)
            .ToList();

        _output.WriteLine($"\nTop 20 Parameter Sets (out of {optimizationResults.Count} tested):");
        _output.WriteLine($"{"Rank",-5} {"Monthly>",-10} {"Weekly>",-10} {"Pullback",-12} {"RSI-P",-6} {"Signals",-9} {"Wins",-6} {"WinRate",-9} {"AvgRet%",-9}");
        _output.WriteLine(new string('-', 80));

        for (int i = 0; i < ranked.Count; i++)
        {
            var r = ranked[i];
            _output.WriteLine(
                $"{i + 1,-5} {r.MonthlyThreshold,-10:F0} {r.WeeklyThreshold,-10:F0} " +
                $"{r.PullbackLow:F0}-{r.PullbackHigh:F0}     {r.RsiPeriod,-6} " +
                $"{r.TotalSignals,-9} {r.Wins,-6} {r.WinRate,-9:F1}% {r.AvgReturn,-9:F2}%");
        }

        Assert.True(true); // Research test
    }

    #region Helpers

    private async Task<BacktestRow?> BacktestSignal(AppDbContext context, ScreenerEntity signal)
    {
        var futurePrices = await context.TickerHistories
            .Where(t => t.Symbol == signal.Symbol && t.Date > signal.RecognizeDate)
            .OrderBy(t => t.Date)
            .Take(45)
            .Select(t => new { t.Date, t.Close, t.High, t.Low })
            .ToListAsync();

        if (futurePrices.Count < 5)
            return null;

        double entryPrice = signal.RecognizedPrice;
        if (entryPrice <= 0)
            return null;

        double return5D = futurePrices.Count >= 5
            ? ((double)futurePrices[4].Close - entryPrice) / entryPrice * 100 : 0;
        double return10D = futurePrices.Count >= 10
            ? ((double)futurePrices[9].Close - entryPrice) / entryPrice * 100 : 0;
        double return20D = futurePrices.Count >= 20
            ? ((double)futurePrices[19].Close - entryPrice) / entryPrice * 100 : 0;
        double return40D = futurePrices.Count >= 40
            ? ((double)futurePrices[39].Close - entryPrice) / entryPrice * 100 : 0;

        double maxGain = 0, maxDrawdown = 0;
        foreach (var p in futurePrices)
        {
            double highRet = ((double)p.High - entryPrice) / entryPrice * 100;
            double lowRet = ((double)p.Low - entryPrice) / entryPrice * 100;
            maxGain = Math.Max(maxGain, highRet);
            maxDrawdown = Math.Min(maxDrawdown, lowRet);
        }

        string outcome = return10D >= WinThresholdPercent ? "Win"
            : return10D <= LossThresholdPercent ? "Loss"
            : "Neutral";

        return new BacktestRow
        {
            ScreenerId = signal.Id,
            Symbol = signal.Symbol,
            StrategyName = signal.ScreenerName,
            TimeFrame = signal.TimeFrame,
            SignalDate = signal.RecognizeDate,
            EntryPrice = entryPrice,
            Return5D = Math.Round(return5D, 2),
            Return10D = Math.Round(return10D, 2),
            Return20D = Math.Round(return20D, 2),
            Return40D = Math.Round(return40D, 2),
            MaxDrawdown = Math.Round(maxDrawdown, 2),
            MaxGain = Math.Round(maxGain, 2),
            Outcome = outcome
        };
    }

    private void PrintLeaderboard(List<BacktestRow> allResults)
    {
        _output.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        _output.WriteLine("║                        STRATEGY LEADERBOARD                                ║");
        _output.WriteLine("╠══════════════════════════════════════════════════════════════════════════════╣");
        _output.WriteLine($"║ {"Strategy",-20} {"Signals",-9} {"Wins",-6} {"Loss",-6} {"WinRate",-9} {"Avg10D%",-9} {"AvgDD%",-9} {"Stars",-6} ║");
        _output.WriteLine("╠══════════════════════════════════════════════════════════════════════════════╣");

        var grouped = allResults
            .GroupBy(r => r.StrategyName)
            .Select(g =>
            {
                int wins = g.Count(r => r.Outcome == "Win");
                int losses = g.Count(r => r.Outcome == "Loss");
                double winRate = (double)wins / g.Count() * 100;
                double avgReturn = g.Average(r => r.Return10D);
                double avgDD = g.Average(r => r.MaxDrawdown);
                int stars = winRate > 65 ? 5 : winRate > 55 ? 4 : winRate > 45 ? 3 : winRate > 30 ? 2 : 1;

                return new
                {
                    Strategy = g.Key,
                    Total = g.Count(),
                    Wins = wins,
                    Losses = losses,
                    WinRate = winRate,
                    AvgReturn = avgReturn,
                    AvgDD = avgDD,
                    Stars = stars
                };
            })
            .OrderByDescending(s => s.WinRate)
            .ToList();

        foreach (var s in grouped)
        {
            string starStr = new string('★', s.Stars) + new string('☆', 5 - s.Stars);
            _output.WriteLine(
                $"║ {s.Strategy,-20} {s.Total,-9} {s.Wins,-6} {s.Losses,-6} " +
                $"{s.WinRate,-9:F1}% {s.AvgReturn,-9:F2}% {s.AvgDD,-9:F2}% {starStr} ║");
        }

        _output.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
    }

    private void PrintStrategyDetail(string strategyName, List<BacktestRow> results)
    {
        int wins = results.Count(r => r.Outcome == "Win");
        int losses = results.Count(r => r.Outcome == "Loss");
        int neutral = results.Count(r => r.Outcome == "Neutral");
        double winRate = results.Count > 0 ? (double)wins / results.Count * 100 : 0;

        _output.WriteLine($"\n--- {strategyName} ---");
        _output.WriteLine($"  Signals: {results.Count}  |  Win: {wins}  |  Loss: {losses}  |  Neutral: {neutral}  |  Win Rate: {winRate:F1}%");
        _output.WriteLine($"  Avg 5D: {results.Average(r => r.Return5D):F2}%  |  Avg 10D: {results.Average(r => r.Return10D):F2}%  |  Avg 20D: {results.Average(r => r.Return20D):F2}%");
        _output.WriteLine($"  Avg MaxDrawdown: {results.Average(r => r.MaxDrawdown):F2}%  |  Avg MaxGain: {results.Average(r => r.MaxGain):F2}%");

        // Best and worst signals
        var best = results.OrderByDescending(r => r.Return10D).FirstOrDefault();
        var worst = results.OrderBy(r => r.Return10D).FirstOrDefault();
        if (best != null)
            _output.WriteLine($"  Best:  {best.Symbol} ({best.SignalDate:yyyy-MM-dd}) → +{best.Return10D:F2}%");
        if (worst != null)
            _output.WriteLine($"  Worst: {worst.Symbol} ({worst.SignalDate:yyyy-MM-dd}) → {worst.Return10D:F2}%");
    }

    private static double GetRsi(List<PriceHistory> priceHistories, int length = 14)
    {
        if (priceHistories.Count < length + 1)
            return 0;

        var rsi = new RSI(length) { PriceHistoryList = priceHistories };
        var lastValue = rsi.Calculate().ResultData.LastOrDefault()?.Value;
        return lastValue.GetValueOrDefault();
    }

    #endregion

    #region Internal DTOs

    private class BacktestRow
    {
        public long ScreenerId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string StrategyName { get; set; } = string.Empty;
        public string TimeFrame { get; set; } = string.Empty;
        public DateTime SignalDate { get; set; }
        public double EntryPrice { get; set; }
        public double Return5D { get; set; }
        public double Return10D { get; set; }
        public double Return20D { get; set; }
        public double Return40D { get; set; }
        public double MaxDrawdown { get; set; }
        public double MaxGain { get; set; }
        public string Outcome { get; set; } = string.Empty;
    }

    private class OptimizationRow
    {
        public double MonthlyThreshold { get; set; }
        public double WeeklyThreshold { get; set; }
        public double PullbackLow { get; set; }
        public double PullbackHigh { get; set; }
        public int RsiPeriod { get; set; }
        public int TotalSignals { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double WinRate { get; set; }
        public double AvgReturn { get; set; }
    }

    #endregion
}
```

## Key Design Decisions in Original Code

1. **Win/Loss Thresholds:** +5% / -3% (asymmetric — higher bar for wins)
2. **Forward window:** Primary evaluation on 10-day return, but 5D/20D/40D also tracked
3. **RSI band filter:** All screeners use 55–70 RSI band for signal detection (except RSITTF which uses its own GFS logic)
4. **Grid search:** Original tested RSI periods [5, 7, 14] — later fixed to 14 only
5. **Minimum signal count:** 3 signals required per parameter set to avoid noise
