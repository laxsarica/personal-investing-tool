using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ScreenEdge.Backtest.Models;
using ScreenEdge.Entity;
using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;
using Ta.Indicator.Indicator;
using TA.Indicators.Indicator;

namespace ScreenEdge.Backtest;

public class WealthCreationBacktestEngine
{
    private readonly string _connectionString;

    public WealthCreationBacktestEngine(string connectionString)
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

    public async Task<BacktestJobResult> RunAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        List<string> symbols;
        using (var context = CreateDbContext())
        {
            symbols = await context.DistinctStocks
                .Where(s => s.Exchange == "NSE" && s.TotalTradingDays >= 365)
                .Select(s => s.Symbol)
                .ToListAsync();
        }

        Console.WriteLine($"Scanning {symbols.Count} NSE stocks for Wealth Creation Strategy...");
        Console.WriteLine(new string('─', 80));

        var allResults = new ConcurrentBag<BacktestRow>();
        int processedCount = 0;
        int signalCount = 0;

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };

        await Parallel.ForEachAsync(symbols, parallelOptions, async (symbol, ct) =>
        {
            try
            {
                var results = await ScanStockHistory(symbol);
                foreach (var r in results) allResults.Add(r);

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

    private async Task<List<BacktestRow>> ScanStockHistory(string symbol)
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

        if (dailyData.Count < 200) return [];

        var dailyEma50Series = CalculateEmaSeries(dailyData, 50);
        var dailyEma200Series = CalculateEmaSeries(dailyData, 200);
        var dailyVolumeMa20Series = CalculateVolumeMaSeries(dailyData, 20);

        var weeklyData = DataConverter.ConvertToWeeklyOHLC(dailyData);
        var weeklyRsiSeries = CalculateRsiSeries(weeklyData, 14);

        // Map weekly RSI values back to the exact Date of the weekly candle close
        var weeklyRsiDict = new Dictionary<DateTime, double>();
        for (int j = 1; j < weeklyRsiSeries.Count; j++)
        {
            if (j < weeklyData.Count && weeklyRsiSeries[j] != null && weeklyRsiSeries[j].Value.HasValue)
            {
                weeklyRsiDict[weeklyData[j].Date] = weeklyRsiSeries[j].Value.Value;
            }
        }

        var signals = new List<BacktestRow>();
        double? previousWeeklyRsi = null;

        for (int i = 1; i < dailyData.Count; i++)
        {
            var date = dailyData[i].Date;
            
            // Check if this day is a weekly candle close day that we have RSI for
            if (weeklyRsiDict.TryGetValue(date, out double currentWeeklyRsi))
            {
                // Trend & Volume Filters
                double ema200 = dailyEma200Series.Count > i && dailyEma200Series[i] != null && dailyEma200Series[i].Value.HasValue ? dailyEma200Series[i].Value.Value : 0;
                double volMa20 = dailyVolumeMa20Series.Count > i && dailyVolumeMa20Series[i] != null && dailyVolumeMa20Series[i].Value.HasValue ? dailyVolumeMa20Series[i].Value.Value : 0;
                
                bool isUptrend = ema200 > 0 && dailyData[i].Close > ema200;
                bool isHighVolume = dailyData[i].Volume > volMa20;

                // Entry condition: crossed 60 + uptrend + high volume
                if (previousWeeklyRsi.HasValue && previousWeeklyRsi.Value <= 60 && currentWeeklyRsi > 60 && isUptrend && isHighVolume)
                {
                    double entryPrice = dailyData[i].Close;
                    
                    // Exit condition loop
                    int exitIdx = -1;
                    for (int e = i + 1; e < dailyData.Count; e++)
                    {
                        double ema50 = dailyEma50Series.Count > e && dailyEma50Series[e] != null && dailyEma50Series[e].Value.HasValue 
                            ? dailyEma50Series[e].Value.Value : 0;
                            
                        if (ema50 > 0 && dailyData[e].Close < ema50)
                        {
                            exitIdx = e;
                            break;
                        }
                    }

                    DateTime? exitDate = null;
                    double? exitPrice = null;
                    int? daysHeld = null;
                    double? realizedReturn = null;
                    string outcome = "Neutral"; // "Open" trade

                    if (exitIdx != -1)
                    {
                        exitDate = dailyData[exitIdx].Date;
                        exitPrice = dailyData[exitIdx].Close;
                        daysHeld = (exitDate.Value - date).Days;
                        realizedReturn = (exitPrice.Value - entryPrice) / entryPrice * 100;

                        outcome = realizedReturn > 0 ? "Win" : "Loss";
                    }

                    double maxGain = 0, maxDrawdown = 0;
                    int bound = exitIdx != -1 ? exitIdx : dailyData.Count - 1;
                    for(int m = i + 1; m <= bound; m++)
                    {
                        maxGain = Math.Max(maxGain, (dailyData[m].High - entryPrice) / entryPrice * 100);
                        maxDrawdown = Math.Min(maxDrawdown, (dailyData[m].Low - entryPrice) / entryPrice * 100);
                    }

                    signals.Add(new BacktestRow
                    {
                        ScreenerId = 0,
                        Symbol = symbol,
                        StrategyName = "WealthCreation",
                        TimeFrame = "W",
                        SignalDate = date,
                        EntryPrice = Math.Round(entryPrice, 2),
                        RsiDaily = 0,
                        RsiWeekly = Math.Round(currentWeeklyRsi, 2),
                        RsiMonthly = 0,
                        Volume = (long)dailyData[i].Volume,
                        Pattern = "",
                        Return5D = 0, Return10D = 0, Return20D = 0, Return40D = 0,
                        MaxDrawdown = Math.Round(maxDrawdown, 2),
                        MaxGain = Math.Round(maxGain, 2),
                        ExitDate = exitDate,
                        ExitPrice = exitPrice.HasValue ? Math.Round(exitPrice.Value, 2) : null,
                        DaysHeld = daysHeld,
                        RealizedReturn = realizedReturn.HasValue ? Math.Round(realizedReturn.Value, 2) : null,
                        Outcome = outcome
                    });
                }

                previousWeeklyRsi = currentWeeklyRsi;
            }
        }

        return signals;
    }

    private static List<TimeSeriesData> CalculateRsiSeries(List<PriceHistory> data, int period)
    {
        if (data.Count < period + 1) return [];
        return new RSI(period) { PriceHistoryList = data }.Calculate().ResultData;
    }

    private static List<TimeSeriesData> CalculateEmaSeries(List<PriceHistory> data, int period)
    {
        if (data.Count < period) return [];
        return new EMA(period) { PriceHistoryList = data }.Calculate().ResultData;
    }

    private static List<TimeSeriesData> CalculateVolumeMaSeries(List<PriceHistory> data, int period)
    {
        if (data.Count < period) return [];
        return new VolumeMA(period) { PriceHistoryList = data }.Calculate().ResultData;
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
                        AvgMaxDrawdown = Math.Round(g.Average(r => r.MaxDrawdown), 2),
                        AvgMaxGain = Math.Round(g.Average(r => r.MaxGain), 2),
                        AvgDaysHeld = Math.Round(avgDaysHeld, 1),
                        AvgRealizedReturn = Math.Round(avgRealizedReturn, 2),
                        Stars = decisiveWinRate > 65 ? 5 : decisiveWinRate > 55 ? 4 : decisiveWinRate > 45 ? 3 : decisiveWinRate > 30 ? 2 : 1
                    };
                });
    }
}
