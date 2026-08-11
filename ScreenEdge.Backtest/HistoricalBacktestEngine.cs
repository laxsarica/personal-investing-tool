using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ScreenEdge.Backtest.Models;
using ScreenEdge.Entity;
using Ta.CustomIndicator.ChandelierExit;
using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;
using TA.Indicators.Indicator;

namespace ScreenEdge.Backtest;

/// <summary>
/// Historical backtest engine — Dynamic Chandelier Exit Implementation
/// Scans ALL stocks through their full price history to find signals,
/// then uses ChandelierExitOscillator to dynamically manage the trade until exit.
/// </summary>
public class HistoricalBacktestEngine
{
    private readonly string _connectionString;
    // Keeping thresholds here as fallbacks, but we use RealizedReturn > 0 for dynamic wins
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

    public async Task<BacktestJobResult> RunHistoricalRsiTtfAsync(
        double monthlyThreshold = 60.0,
        double weeklyThreshold = 60.0,
        double pullbackLow = 35.0,
        double pullbackHigh = 45.0,
        int minForwardBars = 10)
    {
        var stopwatch = Stopwatch.StartNew();

        List<string> symbols;
        List<PriceHistory> niftyData;
        using (var context = CreateDbContext())
        {
            symbols = await context.DistinctStocks
                .Where(s => s.Exchange == "NSE" && s.TotalTradingDays >= 365)
                .Select(s => s.Symbol)
                .ToListAsync();

            niftyData = await context.TickerHistories
                .Where(t => t.Symbol == "NIFTYBEES")
                .OrderBy(t => t.Date)
                .Select(h => new PriceHistory { Date = h.Date, Close = (double)h.Close })
                .ToListAsync();
        }

        var niftySmaSeries = CalculateSmaSeries(niftyData, 200);
        var niftySmaLookup = BuildSmaLookup(niftyData, niftySmaSeries);

        Console.WriteLine($"Scanning {symbols.Count} NSE stocks with 365+ trading days (Dynamic Exit)...");
        Console.WriteLine($"RSITTF Params: Monthly>{monthlyThreshold}, Weekly>{weeklyThreshold}, Pullback={pullbackLow}-{pullbackHigh}");
        Console.WriteLine(new string('─', 80));

        var allResults = new ConcurrentBag<BacktestRow>();
        int processedCount = 0;
        int signalCount = 0;

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };

        await Parallel.ForEachAsync(symbols, parallelOptions, async (symbol, ct) =>
        {
            try
            {
                var results = await ScanStockHistory(symbol, monthlyThreshold, weeklyThreshold,
                    pullbackLow, pullbackHigh, minForwardBars, niftyData, niftySmaLookup);

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
        Console.WriteLine($"Scan complete: {resultList.Count} signals found across {symbols.Count} stocks in {stopwatch.Elapsed.TotalSeconds:F1}s");

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

    private async Task<List<BacktestRow>> ScanStockHistory(
        string symbol,
        double monthlyThreshold,
        double weeklyThreshold,
        double pullbackLow,
        double pullbackHigh,
        int minForwardBars,
        List<PriceHistory> niftyData,
        Dictionary<DateTime, double> niftySmaLookup)
    {
        using var context = CreateDbContext();

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

        if (dailyData.Count < 365) return [];

        var dailyRsiSeries = CalculateRsiSeries(dailyData, 14);
        var dailySma50Series = CalculateSmaSeries(dailyData, 50);

        // Pre-calculate dynamic Chandelier Exit for the whole history
        var chandelierExit = new ChandelierExitOscillator().Calculate(dailyData)
            .GroupBy(x => x.Date)
            .ToDictionary(g => g.Key, g => g.Last());

        var weeklyData = DataConverter.ConvertToWeeklyOHLC(dailyData);
        var monthlyData = DataConverter.ConvertToMonthlyOHLC(dailyData);

        var weeklyRsiSeries = CalculateRsiSeries(weeklyData, 14);
        var monthlyRsiSeries = CalculateRsiSeries(monthlyData, 14);

        var weeklyRsiLookup = BuildTrendPersistenceLookup(dailyData, weeklyData, weeklyRsiSeries, weeklyThreshold);
        var monthlyRsiLookup = BuildTrendPersistenceLookup(dailyData, monthlyData, monthlyRsiSeries, monthlyThreshold);

        var signals = new List<BacktestRow>();

        for (int i = 1; i < dailyRsiSeries.Count && (i + 1 + minForwardBars) < dailyData.Count; i++)
        {
            int dailyIdx = i + 1;
            var date = dailyData[dailyIdx].Date;
            
            double prevRsi = dailyRsiSeries[i - 1]?.Value ?? 0;
            double currRsi = dailyRsiSeries[i]?.Value ?? 0;

            if (currRsi <= prevRsi || currRsi < pullbackLow || currRsi > pullbackHigh)
                continue;

            if (!weeklyRsiLookup.TryGetValue(date, out var weeklyInfo) || 
                !monthlyRsiLookup.TryGetValue(date, out var monthlyInfo))
                continue;

            if (monthlyInfo.Rsi <= monthlyThreshold || monthlyInfo.ConsecutivePeriods < 2) continue;
            if (weeklyInfo.Rsi <= weeklyThreshold || weeklyInfo.ConsecutivePeriods < 3) continue;

            double dailySma50 = dailySma50Series.Count > i && dailySma50Series[i] != null && dailySma50Series[i].Value.HasValue 
                ? dailySma50Series[i].Value.Value : 0;
            if (dailySma50 == 0 || dailyData[dailyIdx].Close <= dailySma50)
                continue;

            double niftyClose = GetClosestPreviousClose(niftyData, date);
            double niftySma = GetClosestPreviousSma(niftySmaLookup, date);
            if (niftySma == 0 || niftyClose <= niftySma)
                continue;

            double entryPrice = dailyData[dailyIdx].Close;
            if (entryPrice <= 0) continue;

            var forwardBars = dailyData.Skip(dailyIdx + 1).Take(45).ToList();
            if (forwardBars.Count == 0) continue;

            double return5D = forwardBars.Count >= 5 ? (forwardBars[4].Close - entryPrice) / entryPrice * 100 : 0;
            double return10D = forwardBars.Count >= 10 ? (forwardBars[9].Close - entryPrice) / entryPrice * 100 : 0;
            double return20D = forwardBars.Count >= 20 ? (forwardBars[19].Close - entryPrice) / entryPrice * 100 : 0;
            double return40D = forwardBars.Count >= 40 ? (forwardBars[39].Close - entryPrice) / entryPrice * 100 : 0;

            double maxGain = 0, maxDrawdown = 0;
            foreach (var bar in forwardBars)
            {
                maxGain = Math.Max(maxGain, (bar.High - entryPrice) / entryPrice * 100);
                maxDrawdown = Math.Min(maxDrawdown, (bar.Low - entryPrice) / entryPrice * 100);
            }

            // Dynamic Exit Logic
            int exitIdx = -1;
            for (int e = dailyIdx + 1; e < dailyData.Count; e++)
            {
                // Exit if trend flips to bearish or direction changed
                if (chandelierExit.TryGetValue(dailyData[e].Date, out var ce))
                {
                    if (ce.Direction == -1 || ce.DirectionChanged)
                    {
                        exitIdx = e;
                        break;
                    }
                }
            }

            DateTime? exitDate = null;
            double? exitPrice = null;
            int? daysHeld = null;
            double? realizedReturn = null;
            string outcome = "Neutral"; // "Open" trade if we hit end of data without exit

            if (exitIdx != -1)
            {
                exitDate = dailyData[exitIdx].Date;
                exitPrice = dailyData[exitIdx].Close;
                daysHeld = (exitDate.Value - date).Days;
                realizedReturn = (exitPrice.Value - entryPrice) / entryPrice * 100;

                outcome = realizedReturn > 0 ? "Win" : "Loss";
            }

            signals.Add(new BacktestRow
            {
                ScreenerId = 0,
                Symbol = symbol,
                StrategyName = "RSITTF",
                TimeFrame = "D",
                SignalDate = date,
                EntryPrice = Math.Round(entryPrice, 2),
                RsiDaily = Math.Round(currRsi, 2),
                RsiWeekly = Math.Round(weeklyInfo.Rsi, 2),
                RsiMonthly = Math.Round(monthlyInfo.Rsi, 2),
                Volume = (long)dailyData[dailyIdx].Volume,
                Pattern = "",
                Return5D = Math.Round(return5D, 2),
                Return10D = Math.Round(return10D, 2),
                Return20D = Math.Round(return20D, 2),
                Return40D = Math.Round(return40D, 2),
                MaxDrawdown = Math.Round(maxDrawdown, 2),
                MaxGain = Math.Round(maxGain, 2),
                ExitDate = exitDate,
                ExitPrice = exitPrice.HasValue ? Math.Round(exitPrice.Value, 2) : null,
                DaysHeld = daysHeld,
                RealizedReturn = realizedReturn.HasValue ? Math.Round(realizedReturn.Value, 2) : null,
                Outcome = outcome
            });
        }

        return signals;
    }

    private static List<TimeSeriesData> CalculateRsiSeries(List<PriceHistory> data, int period)
    {
        if (data.Count < period + 1) return [];
        return new RSI(period) { PriceHistoryList = data }.Calculate().ResultData;
    }

    private static List<TimeSeriesData> CalculateSmaSeries(List<PriceHistory> data, int period)
    {
        if (data.Count < period) return [];
        return new SMA(period) { PriceHistoryList = data }.Calculate().ResultData;
    }

    private static Dictionary<DateTime, double> BuildSmaLookup(List<PriceHistory> data, List<TimeSeriesData> smaSeries)
    {
        var lookup = new Dictionary<DateTime, double>();
        for (int i = 0; i < data.Count && i < smaSeries.Count; i++)
        {
            if (smaSeries[i] != null && smaSeries[i].Value.HasValue)
                lookup[data[i].Date] = smaSeries[i].Value.Value;
        }
        return lookup;
    }

    private double GetClosestPreviousClose(List<PriceHistory> data, DateTime date)
    {
        for (int i = data.Count - 1; i >= 0; i--)
        {
            if (data[i].Date <= date) return data[i].Close;
        }
        return 0;
    }

    private double GetClosestPreviousSma(Dictionary<DateTime, double> lookup, DateTime date)
    {
        for (int i = 0; i < 10; i++)
        {
            var d = date.AddDays(-i);
            if (lookup.TryGetValue(d, out var val)) return val;
        }
        return 0;
    }

    private static Dictionary<DateTime, (double Rsi, int ConsecutivePeriods)> BuildTrendPersistenceLookup(
        List<PriceHistory> dailyData,
        List<PriceHistory> higherTFData,
        List<TimeSeriesData> higherTFRsi,
        double threshold)
    {
        var lookup = new Dictionary<DateTime, (double, int)>();
        var rsiByDate = new List<(DateTime Date, double Rsi, int Consecutive)>();

        int consecutive = 0;
        for (int j = 0; j < higherTFRsi.Count; j++)
        {
            int tfIdx = j + 1; // RSI series skips first bar
            if (tfIdx < higherTFData.Count && higherTFRsi[j] != null && higherTFRsi[j].Value.HasValue)
            {
                double rsi = higherTFRsi[j].Value.Value;
                if (rsi > threshold) consecutive++;
                else consecutive = 0;

                rsiByDate.Add((higherTFData[tfIdx].Date, rsi, consecutive));
            }
        }

        int ptr = 0;
        (double Rsi, int Consecutive) lastVal = (0, 0);

        foreach (var day in dailyData)
        {
            while (ptr < rsiByDate.Count && rsiByDate[ptr].Date <= day.Date)
            {
                lastVal = (rsiByDate[ptr].Rsi, rsiByDate[ptr].Consecutive);
                ptr++;
            }
            lookup[day.Date] = lastVal;
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
                    int neutral = g.Count(r => r.Outcome == "Neutral"); // "Neutral" now means the trade is still Open
                    
                    double winRate = g.Count() > 0 ? (double)wins / g.Count() * 100 : 0;
                    
                    int decisiveTotal = wins + losses;
                    double decisiveWinRate = decisiveTotal > 0 ? (double)wins / decisiveTotal * 100 : 0;
                    
                    var closedTrades = g.Where(r => r.ExitDate.HasValue).ToList();
                    double avgDaysHeld = closedTrades.Count > 0 ? closedTrades.Average(r => r.DaysHeld.Value) : 0;
                    double avgRealizedReturn = closedTrades.Count > 0 ? closedTrades.Average(r => r.RealizedReturn.Value) : 0;

                    return new StrategyStats
                    {
                        Total = g.Count(),
                        Wins = wins,
                        Losses = losses,
                        Neutral = neutral,
                        WinRate = Math.Round(winRate, 1),
                        DecisiveWinRate = Math.Round(decisiveWinRate, 1),
                        AvgReturn5D = Math.Round(g.Average(r => r.Return5D), 2),
                        AvgReturn10D = Math.Round(g.Average(r => r.Return10D), 2),
                        AvgReturn20D = Math.Round(g.Average(r => r.Return20D), 2),
                        AvgMaxDrawdown = Math.Round(g.Average(r => r.MaxDrawdown), 2),
                        AvgMaxGain = Math.Round(g.Average(r => r.MaxGain), 2),
                        AvgDaysHeld = Math.Round(avgDaysHeld, 1),
                        AvgRealizedReturn = Math.Round(avgRealizedReturn, 2),
                        Stars = decisiveWinRate > 65 ? 5 : decisiveWinRate > 55 ? 4 : decisiveWinRate > 45 ? 3 : decisiveWinRate > 30 ? 2 : 1
                    };
                });
    }
}
