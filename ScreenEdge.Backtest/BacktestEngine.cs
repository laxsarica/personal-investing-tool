using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ScreenEdge.Backtest.Models;
using ScreenEdge.Entity;
using ScreenerEntity = ScreenEdge.Entity.Entities.Screener;

namespace ScreenEdge.Backtest;

/// <summary>
/// Core backtest engine — reads historical signals from the Screeners table,
/// calculates forward returns from TickerHistories, and produces BacktestRow results.
/// </summary>
public class BacktestEngine
{
    private readonly string _connectionString;
    private const double WinThresholdPercent = 5.0;
    private const double LossThresholdPercent = -3.0;

    public BacktestEngine(string connectionString)
    {
        _connectionString = connectionString;
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Backtest all strategies and produce a full job result with leaderboard data.
    /// </summary>
    public async Task<BacktestJobResult> RunLeaderboardAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        using var context = CreateDbContext();

        var signals = await context.Screeners
            .OrderBy(s => s.RecognizeDate)
            .ToListAsync();

        Console.WriteLine($"Total signals to backtest: {signals.Count}");

        var results = new List<BacktestRow>();

        foreach (var signal in signals)
        {
            var row = await BacktestSignal(context, signal);
            if (row != null)
                results.Add(row);
        }

        stopwatch.Stop();

        var jobResult = new BacktestJobResult
        {
            RunDate = DateTime.Now,
            TotalSignals = signals.Count,
            BacktestedSignals = results.Count,
            SkippedSignals = signals.Count - results.Count,
            ElapsedSeconds = stopwatch.Elapsed.TotalSeconds,
            Results = results,
            StrategyBreakdown = BuildStrategyBreakdown(results)
        };

        Console.WriteLine($"Backtested {results.Count} signals (skipped {jobResult.SkippedSignals}) in {stopwatch.Elapsed.TotalSeconds:F1}s");
        return jobResult;
    }

    /// <summary>
    /// Backtest a single strategy with detailed per-signal output.
    /// </summary>
    public async Task<BacktestJobResult> RunStrategyDetailAsync(string strategyName)
    {
        var stopwatch = Stopwatch.StartNew();
        using var context = CreateDbContext();

        var signals = await context.Screeners
            .Where(s => s.ScreenerName == strategyName)
            .OrderBy(s => s.RecognizeDate)
            .ToListAsync();

        Console.WriteLine($"{strategyName} signals to backtest: {signals.Count}");

        var results = new List<BacktestRow>();

        foreach (var signal in signals)
        {
            var row = await BacktestSignal(context, signal);
            if (row != null)
                results.Add(row);
        }

        stopwatch.Stop();

        return new BacktestJobResult
        {
            RunDate = DateTime.Now,
            TotalSignals = signals.Count,
            BacktestedSignals = results.Count,
            SkippedSignals = signals.Count - results.Count,
            ElapsedSeconds = stopwatch.Elapsed.TotalSeconds,
            Results = results,
            StrategyBreakdown = BuildStrategyBreakdown(results)
        };
    }

    /// <summary>
    /// Calculate forward returns for a single screener signal.
    /// Returns null if insufficient forward price data.
    /// </summary>
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
            RsiDaily = signal.Rsi,
            RsiWeekly = signal.RsiWeekly,
            RsiMonthly = signal.RsiMonthly,
            Volume = signal.Volume,
            Pattern = signal.Pattern,
            Return5D = Math.Round(return5D, 2),
            Return10D = Math.Round(return10D, 2),
            Return20D = Math.Round(return20D, 2),
            Return40D = Math.Round(return40D, 2),
            MaxDrawdown = Math.Round(maxDrawdown, 2),
            MaxGain = Math.Round(maxGain, 2),
            Outcome = outcome
        };
    }

    /// <summary>
    /// Build per-strategy stats from backtest results.
    /// </summary>
    private static Dictionary<string, StrategyStats> BuildStrategyBreakdown(List<BacktestRow> results)
    {
        return results
            .GroupBy(r => r.StrategyName)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    int wins = g.Count(r => r.Outcome == "Win");
                    int losses = g.Count(r => r.Outcome == "Loss");
                    int neutral = g.Count(r => r.Outcome == "Neutral");
                    double winRate = g.Count() > 0 ? (double)wins / g.Count() * 100 : 0;

                    return new StrategyStats
                    {
                        Total = g.Count(),
                        Wins = wins,
                        Losses = losses,
                        Neutral = neutral,
                        WinRate = Math.Round(winRate, 1),
                        AvgReturn5D = Math.Round(g.Average(r => r.Return5D), 2),
                        AvgReturn10D = Math.Round(g.Average(r => r.Return10D), 2),
                        AvgReturn20D = Math.Round(g.Average(r => r.Return20D), 2),
                        AvgMaxDrawdown = Math.Round(g.Average(r => r.MaxDrawdown), 2),
                        AvgMaxGain = Math.Round(g.Average(r => r.MaxGain), 2),
                        Stars = winRate > 65 ? 5 : winRate > 55 ? 4 : winRate > 45 ? 3 : winRate > 30 ? 2 : 1
                    };
                });
    }
}
