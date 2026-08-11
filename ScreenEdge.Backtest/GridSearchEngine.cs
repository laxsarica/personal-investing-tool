using Microsoft.EntityFrameworkCore;
using ScreenEdge.Backtest.Models;
using ScreenEdge.Entity;

namespace ScreenEdge.Backtest;

/// <summary>
/// Grid search engine for RSITTF parameter optimization.
/// Reads signals exclusively from the Screeners table — no on-the-fly raw data scanning.
/// Filters signals by RSI thresholds and evaluates forward returns.
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

    /// <summary>
    /// Run grid search across all parameter combinations.
    /// Uses existing RSITTF signals from the Screeners table and filters by threshold criteria.
    /// </summary>
    public async Task<List<OptimizationRow>> RunGridSearchAsync(GridSearchParameters parameters)
    {
        using var context = CreateDbContext();

        // Load all RSITTF signals with their RSI values from the Screeners table
        var allSignals = await context.Screeners
            .Where(s => s.ScreenerName == "RSITTF")
            .OrderBy(s => s.RecognizeDate)
            .ToListAsync();

        Console.WriteLine($"Loaded {allSignals.Count} RSITTF signals from Screeners table");

        // Pre-load forward price data for all signal symbols + dates
        var forwardPriceCache = new Dictionary<(string Symbol, DateTime Date), List<double>>();
        foreach (var signal in allSignals)
        {
            var key = (signal.Symbol, signal.RecognizeDate);
            if (forwardPriceCache.ContainsKey(key))
                continue;

            var futurePrices = await context.TickerHistories
                .Where(t => t.Symbol == signal.Symbol && t.Date > signal.RecognizeDate)
                .OrderBy(t => t.Date)
                .Take(15)
                .Select(t => (double)t.Close)
                .ToListAsync();

            forwardPriceCache[key] = futurePrices;
        }

        Console.WriteLine($"Cached forward prices for {forwardPriceCache.Count} unique signal points");

        var optimizationResults = new List<OptimizationRow>();

        foreach (double monthlyTh in parameters.MonthlyThresholds)
        foreach (double weeklyTh in parameters.WeeklyThresholds)
        foreach (double pullbackLow in parameters.PullbackLows)
        {
            double pullbackHigh = pullbackLow + parameters.PullbackBandWidth;

            // Filter signals that match current threshold criteria
            // Grandfather (Monthly) > threshold, Father (Weekly) > threshold,
            // Son (Daily RSI) is within pullback zone
            var matchingSignals = allSignals
                .Where(s => s.RsiMonthly > monthlyTh
                         && s.RsiWeekly > weeklyTh
                         && s.Rsi >= pullbackLow
                         && s.Rsi <= pullbackHigh)
                .ToList();

            if (matchingSignals.Count == 0)
                continue;

            int wins = 0, losses = 0, neutral = 0;
            double totalReturn = 0;

            foreach (var signal in matchingSignals)
            {
                var key = (signal.Symbol, signal.RecognizeDate);
                if (!forwardPriceCache.TryGetValue(key, out var futurePrices) || futurePrices.Count < 10)
                    continue;

                double ret10D = (futurePrices[9] - signal.RecognizedPrice) / signal.RecognizedPrice * 100;
                totalReturn += ret10D;

                if (ret10D >= parameters.WinThresholdPercent) wins++;
                else if (ret10D <= parameters.LossThresholdPercent) losses++;
                else neutral++;
            }

            int total = wins + losses + neutral;
            if (total < parameters.MinSignals)
                continue;

            optimizationResults.Add(new OptimizationRow
            {
                MonthlyThreshold = monthlyTh,
                WeeklyThreshold = weeklyTh,
                PullbackLow = pullbackLow,
                PullbackHigh = pullbackHigh,
                TotalSignals = total,
                Wins = wins,
                Losses = losses,
                WinRate = Math.Round((double)wins / total * 100, 1),
                AvgReturn = Math.Round(totalReturn / total, 2)
            });
        }

        Console.WriteLine($"Grid search complete: {optimizationResults.Count} parameter sets evaluated");

        return optimizationResults
            .OrderByDescending(r => r.WinRate)
            .ThenByDescending(r => r.TotalSignals)
            .Take(parameters.TopN)
            .ToList();
    }
}
