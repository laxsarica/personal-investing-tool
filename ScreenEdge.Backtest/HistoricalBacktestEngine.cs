using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ScreenEdge.Backtest.Models;
using ScreenEdge.Entity;
using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;
using TA.Indicators.Indicator;

namespace ScreenEdge.Backtest;

/// <summary>
/// Historical backtest engine — scans ALL stocks through their full price history
/// to find where signals WOULD have fired, then calculates forward returns.
/// This doesn't depend on the Screeners table at all — it generates its own signals
/// from raw TickerHistory data.
/// </summary>
public class HistoricalBacktestEngine
{
    private readonly string _connectionString;
    private const double WinThresholdPercent = 5.0;
    private const double LossThresholdPercent = -3.0;

    public HistoricalBacktestEngine(string connectionString)
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
    /// Scan all NSE stocks through their full history, detect RSITTF signals,
    /// and backtest each one with forward returns.
    /// </summary>
    public async Task<BacktestJobResult> RunHistoricalRsiTtfAsync(
        double monthlyThreshold = 60.0,
        double weeklyThreshold = 60.0,
        double pullbackLow = 35.0,
        double pullbackHigh = 45.0,
        int minForwardBars = 10)
    {
        var stopwatch = Stopwatch.StartNew();

        // Get all stock symbols with enough history
        List<string> symbols;
        using (var context = CreateDbContext())
        {
            symbols = await context.DistinctStocks
                .Where(s => s.Exchange == "NSE" && s.TotalTradingDays >= 365)
                .Select(s => s.Symbol)
                .ToListAsync();
        }

        Console.WriteLine($"Scanning {symbols.Count} NSE stocks with 365+ trading days...");
        Console.WriteLine($"RSITTF Params: Monthly>{monthlyThreshold}, Weekly>{weeklyThreshold}, Pullback={pullbackLow}-{pullbackHigh}");
        Console.WriteLine(new string('─', 80));

        var allResults = new ConcurrentBag<BacktestRow>();
        int processedCount = 0;
        int signalCount = 0;

        // Process stocks with limited parallelism
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };

        await Parallel.ForEachAsync(symbols, parallelOptions, async (symbol, ct) =>
        {
            try
            {
                var results = await ScanStockHistory(symbol, monthlyThreshold, weeklyThreshold,
                    pullbackLow, pullbackHigh, minForwardBars);

                foreach (var r in results)
                    allResults.Add(r);

                var current = Interlocked.Increment(ref processedCount);
                Interlocked.Add(ref signalCount, results.Count);

                if (current % 100 == 0 || current == symbols.Count)
                {
                    Console.WriteLine($"  [{current}/{symbols.Count}] Processed... {signalCount} signals found so far");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR: {symbol} — {ex.Message}");
            }
        });

        stopwatch.Stop();

        var resultList = allResults.OrderBy(r => r.SignalDate).ToList();

        Console.WriteLine(new string('─', 80));
        Console.WriteLine($"Scan complete: {resultList.Count} RSITTF signals found across {symbols.Count} stocks in {stopwatch.Elapsed.TotalSeconds:F1}s");

        return new BacktestJobResult
        {
            RunDate = DateTime.Now,
            TotalSignals = resultList.Count,
            BacktestedSignals = resultList.Count,
            SkippedSignals = 0,
            ElapsedSeconds = stopwatch.Elapsed.TotalSeconds,
            Results = resultList,
            StrategyBreakdown = BuildStrategyBreakdown(resultList)
        };
    }

    /// <summary>
    /// Scan a single stock's full history for RSITTF signals and backtest each one.
    /// </summary>
    private async Task<List<BacktestRow>> ScanStockHistory(
        string symbol,
        double monthlyThreshold,
        double weeklyThreshold,
        double pullbackLow,
        double pullbackHigh,
        int minForwardBars)
    {
        using var context = CreateDbContext();

        // Load full daily history
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

        if (dailyData.Count < 365)
            return [];

        // Pre-compute RSI(14) series for the full history — all timeframes use period 14
        var dailyRsiSeries = CalculateRsiSeries(dailyData, 14);

        // Convert to weekly/monthly and compute their RSI(14)
        var weeklyData = DataConverter.ConvertToWeeklyOHLC(dailyData);
        var monthlyData = DataConverter.ConvertToMonthlyOHLC(dailyData);

        var weeklyRsiSeries = CalculateRsiSeries(weeklyData, 14);
        var monthlyRsiSeries = CalculateRsiSeries(monthlyData, 14);

        // Build lookup: daily date → weekly/monthly RSI value
        var weeklyRsiLookup = BuildWeeklyRsiLookup(dailyData, weeklyData, weeklyRsiSeries);
        var monthlyRsiLookup = BuildMonthlyRsiLookup(dailyData, monthlyData, monthlyRsiSeries);

        var signals = new List<BacktestRow>();

        // Walk through daily bars — need at least minForwardBars bars after signal
        // RSI series starts from index 1 of daily data, so dailyRsiSeries[i] corresponds to dailyData[i+1]
        for (int i = 1; i < dailyRsiSeries.Count && (i + 1 + minForwardBars) < dailyData.Count; i++)
        {
            int dailyIdx = i + 1; // RSI series starts from dailyData[1], so offset by 1

            // Check daily RSI(14) crossover above pullback zone
            double prevRsi = dailyRsiSeries[i - 1].Value ?? 0;
            double currRsi = dailyRsiSeries[i].Value ?? 0;

            if (prevRsi >= pullbackLow || currRsi <= pullbackLow || currRsi > pullbackHigh)
                continue; // No crossover through pullback zone

            // Get weekly and monthly RSI at this date
            var date = dailyData[dailyIdx].Date;
            if (!weeklyRsiLookup.TryGetValue(date, out double weeklyRsi) ||
                !monthlyRsiLookup.TryGetValue(date, out double monthlyRsi))
                continue;

            // GFS alignment check
            if (monthlyRsi <= monthlyThreshold || weeklyRsi <= weeklyThreshold)
                continue;

            // Signal found! Calculate forward returns
            double entryPrice = dailyData[dailyIdx].Close;
            if (entryPrice <= 0)
                continue;

            var forwardBars = dailyData.Skip(dailyIdx + 1).Take(45).ToList();
            if (forwardBars.Count < minForwardBars)
                continue;

            double return5D = forwardBars.Count >= 5
                ? (forwardBars[4].Close - entryPrice) / entryPrice * 100 : 0;
            double return10D = forwardBars.Count >= 10
                ? (forwardBars[9].Close - entryPrice) / entryPrice * 100 : 0;
            double return20D = forwardBars.Count >= 20
                ? (forwardBars[19].Close - entryPrice) / entryPrice * 100 : 0;
            double return40D = forwardBars.Count >= 40
                ? (forwardBars[39].Close - entryPrice) / entryPrice * 100 : 0;

            double maxGain = 0, maxDrawdown = 0;
            foreach (var bar in forwardBars)
            {
                double highRet = (bar.High - entryPrice) / entryPrice * 100;
                double lowRet = (bar.Low - entryPrice) / entryPrice * 100;
                maxGain = Math.Max(maxGain, highRet);
                maxDrawdown = Math.Min(maxDrawdown, lowRet);
            }

            string outcome = return10D >= WinThresholdPercent ? "Win"
                : return10D <= LossThresholdPercent ? "Loss"
                : "Neutral";

            double dailyRsi14 = dailyRsiSeries.Count > i ? (dailyRsiSeries[i].Value ?? 0) : 0;

            signals.Add(new BacktestRow
            {
                ScreenerId = 0, // No screener ID — this is a historical scan
                Symbol = symbol,
                StrategyName = "RSITTF",
                TimeFrame = "D",
                SignalDate = date,
                EntryPrice = Math.Round(entryPrice, 2),
                RsiDaily = Math.Round(currRsi, 2),
                RsiWeekly = Math.Round(weeklyRsi, 2),
                RsiMonthly = Math.Round(monthlyRsi, 2),
                Volume = (long)dailyData[dailyIdx].Volume,
                Pattern = "",
                Return5D = Math.Round(return5D, 2),
                Return10D = Math.Round(return10D, 2),
                Return20D = Math.Round(return20D, 2),
                Return40D = Math.Round(return40D, 2),
                MaxDrawdown = Math.Round(maxDrawdown, 2),
                MaxGain = Math.Round(maxGain, 2),
                Outcome = outcome
            });
        }

        return signals;
    }

    /// <summary>
    /// Calculate RSI for a full price series and return the complete result list.
    /// </summary>
    private static List<TimeSeriesData> CalculateRsiSeries(List<PriceHistory> data, int period)
    {
        if (data.Count < period + 1)
            return [];

        var rsi = new RSI(period) { PriceHistoryList = data };
        return rsi.Calculate().ResultData;
    }

    /// <summary>
    /// Build a lookup from daily date → weekly RSI.
    /// For each daily bar, the applicable weekly RSI is from the most recent
    /// completed weekly bar (to avoid look-ahead bias).
    /// </summary>
    private static Dictionary<DateTime, double> BuildWeeklyRsiLookup(
        List<PriceHistory> dailyData,
        List<PriceHistory> weeklyData,
        List<TimeSeriesData> weeklyRsi)
    {
        var lookup = new Dictionary<DateTime, double>();

        // weeklyRsi[j] corresponds to weeklyData[j+1] (RSI starts from index 1)
        // Build a sorted list of (weekEndDate, rsiValue) pairs
        var weeklyRsiByDate = new List<(DateTime Date, double Rsi)>();
        for (int j = 0; j < weeklyRsi.Count; j++)
        {
            // weeklyData index = j + 1 (RSI skips first bar)
            int weekIdx = j + 1;
            if (weekIdx < weeklyData.Count && weeklyRsi[j].Value.HasValue)
            {
                weeklyRsiByDate.Add((weeklyData[weekIdx].Date, weeklyRsi[j].Value.GetValueOrDefault()));
            }
        }

        // For each daily date, find the most recent weekly RSI
        int weekPtr = 0;
        double lastWeeklyRsi = 0;

        foreach (var day in dailyData)
        {
            // Advance week pointer while the next week's date is <= today
            while (weekPtr < weeklyRsiByDate.Count && weeklyRsiByDate[weekPtr].Date <= day.Date)
            {
                lastWeeklyRsi = weeklyRsiByDate[weekPtr].Rsi;
                weekPtr++;
            }
            lookup[day.Date] = lastWeeklyRsi;
        }

        return lookup;
    }

    /// <summary>
    /// Build a lookup from daily date → monthly RSI.
    /// Same approach — most recent completed monthly bar.
    /// </summary>
    private static Dictionary<DateTime, double> BuildMonthlyRsiLookup(
        List<PriceHistory> dailyData,
        List<PriceHistory> monthlyData,
        List<TimeSeriesData> monthlyRsi)
    {
        var lookup = new Dictionary<DateTime, double>();

        var monthlyRsiByDate = new List<(DateTime Date, double Rsi)>();
        for (int j = 0; j < monthlyRsi.Count; j++)
        {
            int monthIdx = j + 1;
            if (monthIdx < monthlyData.Count && monthlyRsi[j].Value.HasValue)
            {
                monthlyRsiByDate.Add((monthlyData[monthIdx].Date, monthlyRsi[j].Value.GetValueOrDefault()));
            }
        }

        int monthPtr = 0;
        double lastMonthlyRsi = 0;

        foreach (var day in dailyData)
        {
            while (monthPtr < monthlyRsiByDate.Count && monthlyRsiByDate[monthPtr].Date <= day.Date)
            {
                lastMonthlyRsi = monthlyRsiByDate[monthPtr].Rsi;
                monthPtr++;
            }
            lookup[day.Date] = lastMonthlyRsi;
        }

        return lookup;
    }

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
