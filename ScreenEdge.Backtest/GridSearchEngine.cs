using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ScreenEdge.Backtest.Models;
using ScreenEdge.Entity;
using Ta.CustomIndicator.ChandelierExit;
using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;
using TA.Indicators.Indicator;

namespace ScreenEdge.Backtest;

/// <summary>
/// Grid search engine for RSITTF parameter optimization using raw TickerHistory.
/// Includes v2 Strategy Filters (Entry Trigger, Trend Persistence, SMA50, Market Regime).
/// </summary>
public class GridSearchEngine
{
    private readonly string _connectionString;

    public GridSearchEngine(string connectionString)
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

    public async Task<List<OptimizationRow>> RunGridSearchAsync(GridSearchParameters parameters)
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

        Console.WriteLine($"[Optimize] Scanning {symbols.Count} NSE stocks for historical grid search (v2)...");
        
        var tallyMap = new ConcurrentDictionary<string, ComboResult>();

        foreach (var m in parameters.MonthlyThresholds)
        foreach (var w in parameters.WeeklyThresholds)
        foreach (var p in parameters.PullbackLows)
        {
            string key = GetKey(m, w, p);
            tallyMap[key] = new ComboResult { Monthly = m, Weekly = w, Pullback = p };
        }

        int processedCount = 0;
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };

        await Parallel.ForEachAsync(symbols, parallelOptions, async (symbol, ct) =>
        {
            try
            {
                await ProcessStockForGridSearch(symbol, parameters, tallyMap, niftyData, niftySmaLookup);

                var current = Interlocked.Increment(ref processedCount);
                if (current % 100 == 0 || current == symbols.Count)
                {
                    Console.WriteLine($"  [{current}/{symbols.Count}] Processed...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR: {symbol} — {ex.Message}");
            }
        });

        stopwatch.Stop();

        var optimizationResults = new List<OptimizationRow>();
        foreach (var kvp in tallyMap)
        {
            var res = kvp.Value;
            int total = res.Wins + res.Losses + res.Neutral;
            
            if (total < parameters.MinSignals)
                continue;
                
            int decisiveTotal = res.Wins + res.Losses;
            double decisiveWinRate = decisiveTotal > 0 ? (double)res.Wins / decisiveTotal * 100 : 0;

            optimizationResults.Add(new OptimizationRow
            {
                MonthlyThreshold = res.Monthly,
                WeeklyThreshold = res.Weekly,
                PullbackLow = res.Pullback,
                PullbackHigh = res.Pullback + parameters.PullbackBandWidth,
                TotalSignals = total,
                Wins = res.Wins,
                Losses = res.Losses,
                WinRate = Math.Round((double)res.Wins / total * 100, 1),
                DecisiveWinRate = Math.Round(decisiveWinRate, 1),
                AvgReturn = Math.Round(res.TotalReturn / total, 2)
            });
        }

        Console.WriteLine($"Grid search complete in {stopwatch.Elapsed.TotalSeconds:F1}s: {optimizationResults.Count} parameter sets evaluated");

        return optimizationResults
            .OrderByDescending(r => r.WinRate)
            .ThenByDescending(r => r.TotalSignals)
            .Take(parameters.TopN)
            .ToList();
    }

    private string GetKey(double m, double w, double p) => $"M_{m}_W_{w}_P_{p}";

    private async Task ProcessStockForGridSearch(
        string symbol, 
        GridSearchParameters parameters, 
        ConcurrentDictionary<string, ComboResult> tallyMap,
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

        if (dailyData.Count < 365) return;

        var dailyRsiSeries = CalculateRsiSeries(dailyData, 14);
        var dailySma50Series = CalculateSmaSeries(dailyData, 50);
        
        var chandelierExit = new ChandelierExitOscillator().Calculate(dailyData)
            .GroupBy(x => x.Date)
            .ToDictionary(g => g.Key, g => g.Last());

        var weeklyData = DataConverter.ConvertToWeeklyOHLC(dailyData);
        var monthlyData = DataConverter.ConvertToMonthlyOHLC(dailyData);

        var weeklyRsiSeries = CalculateRsiSeries(weeklyData, 14);
        var monthlyRsiSeries = CalculateRsiSeries(monthlyData, 14);

        // Pre-build basic lookups - since grid search checks MULTIPLE thresholds, 
        // we can't pre-calculate the consecutive boolean yet. We must store the raw array.
        var weeklyRsiLookup = BuildRsiLookup(dailyData, weeklyData, weeklyRsiSeries);
        var monthlyRsiLookup = BuildRsiLookup(dailyData, monthlyData, monthlyRsiSeries);

        int minForwardBars = 10;

        for (int i = 1; i < dailyRsiSeries.Count && (i + 1 + minForwardBars) < dailyData.Count; i++)
        {
            int dailyIdx = i + 1;
            var date = dailyData[dailyIdx].Date;

            double prevRsi = dailyRsiSeries[i - 1].Value ?? 0;
            double currRsi = dailyRsiSeries[i].Value ?? 0;

            // Filter 1: Entry Trigger - must be turning up
            if (currRsi <= prevRsi) continue;

            // Price Structure check (Filter 3) — Close > SMA(50)
            double dailySma50 = dailySma50Series.Count > i && dailySma50Series[i] != null && dailySma50Series[i].Value.HasValue 
                ? dailySma50Series[i].Value.Value : 0;
            if (dailySma50 == 0 || dailyData[dailyIdx].Close <= dailySma50)
                continue;

            // Market Regime Gate (Filter 4) — NIFTYBEES > SMA(200)
            double niftyClose = GetClosestPreviousClose(niftyData, date);
            double niftySma = GetClosestPreviousSma(niftySmaLookup, date);
            if (niftySma == 0 || niftyClose <= niftySma)
                continue;

            // Find which PullbackLows this matches
            var matchingPullbacks = new List<double>();
            foreach (var p in parameters.PullbackLows)
            {
                if (currRsi >= p && currRsi <= p + parameters.PullbackBandWidth)
                {
                    matchingPullbacks.Add(p);
                }
            }
            if (matchingPullbacks.Count == 0) continue;

            if (!weeklyRsiLookup.TryGetValue(date, out var weeklyInfo) ||
                !monthlyRsiLookup.TryGetValue(date, out var monthlyInfo))
                continue;

            double entryPrice = dailyData[dailyIdx].Close;
            if (entryPrice <= 0) continue;

            int exitIdx = -1;
            for (int e = dailyIdx + 1; e < dailyData.Count; e++)
            {
                if (chandelierExit.TryGetValue(dailyData[e].Date, out var ce))
                {
                    if (ce.Direction == -1 || ce.DirectionChanged)
                    {
                        exitIdx = e;
                        break;
                    }
                }
            }

            if (exitIdx == -1) continue; // Skip open trades for optimization accuracy

            double exitPrice = dailyData[exitIdx].Close;
            double realizedReturn = (exitPrice - entryPrice) / entryPrice * 100;

            bool isWin = realizedReturn > 0;
            bool isLoss = realizedReturn <= 0;

            // Evaluate parameters
            foreach (var p in matchingPullbacks)
            {
                foreach (var m in parameters.MonthlyThresholds)
                {
                    // Filter 2: Trend Persistence for Monthly (2 consecutive months > m)
                    if (monthlyInfo.Rsi <= m || !HasConsecutiveThreshold(monthlyInfo.History, m, 2)) continue;
                    
                    foreach (var w in parameters.WeeklyThresholds)
                    {
                        // Filter 2: Trend Persistence for Weekly (3 consecutive weeks > w)
                        if (weeklyInfo.Rsi <= w || !HasConsecutiveThreshold(weeklyInfo.History, w, 3)) continue;

                        string key = GetKey(m, w, p);
                        if (tallyMap.TryGetValue(key, out var combo))
                        {
                            lock (combo)
                            {
                                if (isWin) combo.Wins++;
                                else if (isLoss) combo.Losses++;
                                
                                combo.TotalReturn += realizedReturn;
                            }
                        }
                    }
                }
            }
        }
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
            if (data[i].Date <= date) return data[i].Close;
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

    private static Dictionary<DateTime, (double Rsi, List<double> History)> BuildRsiLookup(
        List<PriceHistory> dailyData, List<PriceHistory> tfData, List<TimeSeriesData> tfRsi)
    {
        var lookup = new Dictionary<DateTime, (double, List<double>)>();
        var rsiByDate = new List<(DateTime Date, double Rsi, List<double> History)>();

        var historyBuffer = new List<double>();
        for (int j = 0; j < tfRsi.Count; j++)
        {
            int tfIdx = j + 1;
            if (tfIdx < tfData.Count && tfRsi[j].Value.HasValue)
            {
                double rsi = tfRsi[j].Value.Value;
                historyBuffer.Add(rsi);
                
                // Keep only last 5 for efficiency
                if (historyBuffer.Count > 5) historyBuffer.RemoveAt(0);

                rsiByDate.Add((tfData[tfIdx].Date, rsi, new List<double>(historyBuffer)));
            }
        }

        int ptr = 0;
        (double Rsi, List<double> History) lastVal = (0, new List<double>());
        foreach (var day in dailyData)
        {
            while (ptr < rsiByDate.Count && rsiByDate[ptr].Date <= day.Date)
            {
                lastVal = (rsiByDate[ptr].Rsi, rsiByDate[ptr].History);
                ptr++;
            }
            lookup[day.Date] = lastVal;
        }
        return lookup;
    }

    private bool HasConsecutiveThreshold(List<double> history, double threshold, int requiredConsecutive)
    {
        if (history.Count < requiredConsecutive) return false;
        
        for (int i = 0; i < requiredConsecutive; i++)
        {
            if (history[history.Count - 1 - i] <= threshold)
                return false;
        }
        return true;
    }

    private class ComboResult
    {
        public double Monthly { get; set; }
        public double Weekly { get; set; }
        public double Pullback { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Neutral { get; set; }
        public double TotalReturn { get; set; }
    }
}
