using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScreenEdge.Entity;
using Ta.CustomIndicator.BreakOut;
using Ta.CustomIndicator.EmaFifty;
using Ta.CustomIndicator.RsiWma;
using Ta.CustomIndicator.UptrendBot;
using Ta.CustomIndicator.WealthCreation;
using Ta.CustomIndicator.ZeroLag;
using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;
using TA.Indicators.Indicator;
using ScreenerEntity = ScreenEdge.Entity.Entities.Screener;

namespace ScreenEdge.Screener;

public class ScreenerEngine : IScreenerEngine
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScreenerEngine> _logger;

    public ScreenerEngine(IServiceScopeFactory scopeFactory, ILogger<ScreenerEngine> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<ScreenerJobResult> RunScreenerJobAsync(int? limit = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var screeners = new ConcurrentBag<ScreenerEntity>();
        var errors = new ConcurrentBag<string>();

        // Get all distinct stock symbols
        List<string> symbols;
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var query = context.DistinctStocks
                .Where(s => s.Exchange == "NSE" && s.TotalTradingDays >= 21)
                .Select(s => s.Symbol);
                
            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }
                
            symbols = await query.ToListAsync();
        }

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 10 };

        await Parallel.ForEachAsync(symbols, parallelOptions, async (symbol, ct) =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var dailyData = await context.TickerHistories
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
                    .ToListAsync(ct);

                if (dailyData.Count <= 30)
                    return;

                var weeklyOhlc = DataConverter.ConvertToWeeklyOHLC(dailyData);
                var monthlyOhlc = DataConverter.ConvertToMonthlyOHLC(dailyData);

                // Thread-local RSI calculations
                double rsiDaily = GetRsi(dailyData);
                double rsiWeekly = GetRsi(weeklyOhlc);
                double rsiMonthly = GetRsi(monthlyOhlc);

                foreach (var s in EmaFiftyScreener(symbol, dailyData, weeklyOhlc, rsiDaily, rsiWeekly, rsiMonthly))
                    screeners.Add(s);

                foreach (var s in ZeroLagScreener(symbol, dailyData, weeklyOhlc, rsiDaily, rsiWeekly, rsiMonthly))
                    screeners.Add(s);

                foreach (var s in SupportResistanceScreener(symbol, dailyData, rsiDaily, rsiWeekly, rsiMonthly))
                    screeners.Add(s);

                foreach (var s in RsiWeightedMovingAverageScreener(symbol, dailyData, rsiDaily, rsiWeekly, rsiMonthly))
                    screeners.Add(s);

                foreach (var s in RsiTtfScreener(symbol, dailyData, rsiDaily, rsiWeekly, rsiMonthly))
                    screeners.Add(s);

                foreach (var s in FullRsiScreener(symbol, dailyData, rsiDaily, rsiWeekly, rsiMonthly))
                    screeners.Add(s);

                foreach (var s in UptrendBotScreener(symbol, dailyData, weeklyOhlc, rsiDaily, rsiWeekly, rsiMonthly))
                    screeners.Add(s);

                foreach (var s in WealthCreationScreener(symbol, dailyData, rsiDaily, rsiWeekly, rsiMonthly))
                    screeners.Add(s);
            }
            catch (Exception ex)
            {
                errors.Add($"{symbol}: {ex.Message}");
                _logger.LogError(ex, "Error processing {Symbol}", symbol);
            }
        });

    // Persist results
        var validResults = screeners.Where(x => x != null).ToList();

        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Delete ALL existing screener results for the dates we are inserting to prevent duplicates
            var datesToDelete = validResults.Select(x => x.RecognizeDate).Distinct().ToList();
            var existingScreeners = await context.Screeners
                .Where(s => datesToDelete.Contains(s.RecognizeDate))
                .ToListAsync();
            if (existingScreeners.Count > 0)
            {
                context.Screeners.RemoveRange(existingScreeners);
                _logger.LogInformation("Removed {Count} existing screener records for the processed dates.",
                    existingScreeners.Count);
            }

            context.Screeners.AddRange(validResults);
            await context.SaveChangesAsync();
        }

        stopwatch.Stop();

        var result = new ScreenerJobResult
        {
            TimeMinutes = stopwatch.Elapsed.TotalMinutes,
            RecordCount = validResults.Count,
            TotalStocksScanned = symbols.Count,
            Status = errors.IsEmpty ? "Completed" : "PartialSuccess",
            Errors = errors.ToList(),
            SignalsByStrategy = validResults
                .GroupBy(s => s.ScreenerName)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        _logger.LogInformation("Screener job completed: {Count} signals in {Time:F2} minutes",
            result.RecordCount, result.TimeMinutes);

        return result;
    }

    private static double GetRsi(List<PriceHistory> priceHistories, int length = 14)
    {
        if (priceHistories.Count < length + 1)
            return 0;

        var rsi = new RSI(length);
        rsi.PriceHistoryList = priceHistories;
        var lastValue = rsi.Calculate().ResultData.LastOrDefault()?.Value;
        return lastValue.GetValueOrDefault();
    }

    private static List<ScreenerEntity> UptrendBotScreener(
        string symbol, List<PriceHistory> daily, List<PriceHistory> weekly,
        double rsiDaily, double rsiWeekly, double rsiMonthly)
    {
        var source = new List<ScreenerEntity>();
        try
        {
            var uptrendBot = new UptrendBotIndicator { AtrPeriod = 11, Sensitivity = 2.0 };

            // Weekly scan
            var weeklyResults = uptrendBot.Calculate(weekly).TakeLast(1).ToList();
            if (weeklyResults.Count == 1 && rsiWeekly > 55.0)
            {
                var priceHistory = weekly.Last();
                if (weeklyResults[0].BuySignal)
                {
                    source.Add(CreateScreener(symbol, StrategyEnum.UPTRENDBOT, "W",
                        priceHistory, rsiDaily, rsiWeekly, rsiMonthly));
                }
            }

            // Daily scan
            var uptrendBotDaily = new UptrendBotIndicator { AtrPeriod = 11, Sensitivity = 2.0 };
            var dailyResults = uptrendBotDaily.Calculate(daily).TakeLast(1).ToList();
            if (dailyResults.Count == 1 && rsiDaily > 55.0)
            {
                var priceHistory = daily.Last();
                if (dailyResults[0].BuySignal)
                {
                    source.Add(CreateScreener(symbol, StrategyEnum.UPTRENDBOT, "D",
                        priceHistory, rsiDaily, rsiWeekly, rsiMonthly));
                }
            }
        }
        catch (Exception) { }

        return source.Where(w => w.Rsi >= 55.0 && w.Rsi <= 70.0).ToList();
    }

    private static List<ScreenerEntity> ZeroLagScreener(
        string symbol, List<PriceHistory> daily, List<PriceHistory> weekly,
        double rsiDaily, double rsiWeekly, double rsiMonthly)
    {
        var source = new List<ScreenerEntity>();
        try
        {
            var zeroLagIndicator = new ZeroLagIndicator();

            // Weekly scan
            var weeklyResults = zeroLagIndicator.Calculate(weekly).TakeLast(2).ToList();
            if (weeklyResults.Count == 2 && rsiWeekly > 55.0)
            {
                var priceHistory = weekly.Last();
                if (!weeklyResults[0].UpSignal && weeklyResults[1].UpSignal)
                {
                    source.Add(CreateScreener(symbol, StrategyEnum.NOLAG, "W",
                        priceHistory, rsiDaily, rsiWeekly, rsiMonthly));
                }
            }

            // Daily scan (fresh instance to avoid state accumulation)
            var zeroLagDaily = new ZeroLagIndicator();
            var dailyResults = zeroLagDaily.Calculate(daily).TakeLast(2).ToList();
            if (dailyResults.Count == 2 && rsiDaily > 55.0)
            {
                var priceHistory = daily.Last();
                if (!dailyResults[0].UpSignal && dailyResults[1].UpSignal)
                {
                    source.Add(CreateScreener(symbol, StrategyEnum.NOLAG, "D",
                        priceHistory, rsiDaily, rsiWeekly, rsiMonthly));
                }
            }
        }
        catch (Exception) { }

        return source.Where(w => w.Rsi >= 55.0 && w.Rsi <= 70.0).ToList();
    }

    private static List<ScreenerEntity> EmaFiftyScreener(
        string symbol, List<PriceHistory> daily, List<PriceHistory> weekly,
        double rsiDaily, double rsiWeekly, double rsiMonthly)
    {
        var source = new List<ScreenerEntity>();
        try
        {
            var emaFiftyIndicator = new EmaFiftyIndicator();

            // Weekly scan
            var weeklyResults = emaFiftyIndicator.Calculate(weekly);
            if (weeklyResults.Count > 0 && rsiWeekly > 55.0)
            {
                if (weeklyResults.First().UpSignal)
                {
                    source.Add(CreateScreener(symbol, StrategyEnum.EMAFIFTY, "W",
                        weekly.Last(), rsiDaily, rsiWeekly, rsiMonthly));
                }
            }

            // Daily scan
            var dailyResults = new EmaFiftyIndicator().Calculate(daily);
            if (dailyResults.Count > 0 && rsiDaily > 55.0)
            {
                if (dailyResults.First().UpSignal)
                {
                    source.Add(CreateScreener(symbol, StrategyEnum.EMAFIFTY, "D",
                        daily.Last(), rsiDaily, rsiWeekly, rsiMonthly));
                }
            }
        }
        catch (Exception) { }

        return source.Where(w => w.Rsi >= 55.0 && w.Rsi <= 70.0).ToList();
    }

    private static List<ScreenerEntity> SupportResistanceScreener(
        string symbol, List<PriceHistory> daily,
        double rsiDaily, double rsiWeekly, double rsiMonthly)
    {
        var source = new List<ScreenerEntity>();
        try
        {
            if (daily.Count > 20)
            {
                var supportResistance = new SupportResistanceBreakOutIndicator();
                var results = supportResistance.Calculate(daily);
                if (results.Count > 0 && rsiDaily > 54.0)
                {
                    var maxDate = results.Max(m => m.Date);
                    var signalInLastTwoDays = maxDate >= daily.Last().Date.AddDays(-2);
                    if (results.Last().UpSignal && signalInLastTwoDays)
                    {
                        source.Add(CreateScreener(symbol, StrategyEnum.SUPPORTRESISTANCE, "D",
                            daily.Last(), rsiDaily, rsiWeekly, rsiMonthly));
                    }
                }
            }
        }
        catch (Exception) { }

        return source;
    }

    private static List<ScreenerEntity> RsiWeightedMovingAverageScreener(
        string symbol, List<PriceHistory> daily,
        double rsiDaily, double rsiWeekly, double rsiMonthly)
    {
        var source = new List<ScreenerEntity>();
        try
        {
            if (daily.Count > 55)
            {
                var rsiWma = new RsiWeightedMovingAverageIndicator();
                var result = rsiWma.Calculate(daily);
                if (result.Count > 0 && result.Last().BullishCross && rsiDaily > 55.0)
                {
                    source.Add(CreateScreener(symbol, StrategyEnum.RSIWMA, "D",
                        daily.Last(), rsiDaily, rsiWeekly, rsiMonthly));
                }
            }
        }
        catch (Exception) { }

        return source.Where(w => w.Rsi >= 55.0 && w.Rsi <= 70.0).ToList();
    }

    private static List<ScreenerEntity> RsiTtfScreener(
        string symbol, List<PriceHistory> daily, 
        double rsiDaily, double rsiWeekly, double rsiMonthly)
    {
        var source = new List<ScreenerEntity>();
        double gfsThreshold = 60.0;   // Grandfather + Father must confirm strong uptrend
        double pullbackThreshold = 40.0; // Son (daily) pullback zone
        try
        {
            if (daily.Count > 365)
            {
                var rsi14Son = new RSI(14) { PriceHistoryList = daily };
                var dailyRsi14Son = rsi14Son.Calculate().ResultData.TakeLast(2).ToList();
                
                if (rsiMonthly > gfsThreshold && rsiWeekly > gfsThreshold)
                {
                    if (dailyRsi14Son.Count == 2 && dailyRsi14Son[0].Value < pullbackThreshold && dailyRsi14Son[1].Value > pullbackThreshold)
                    {
                        var s = CreateScreener(symbol, StrategyEnum.RSITTF, "D", daily.Last(), rsiDaily, rsiWeekly, rsiMonthly);
                        s.Rsi = dailyRsi14Son[1].Value ?? 0;
                        source.Add(s);
                    }
                }
            }
        }
        catch (Exception) { }
        return source;
    }

    private static List<ScreenerEntity> FullRsiScreener(
        string symbol, List<PriceHistory> daily, 
        double rsiDaily, double rsiWeekly, double rsiMonthly)
    {
        var source = new List<ScreenerEntity>();
        try
        {
            if (daily.Count > 14 && rsiDaily < 45.0)
            {
                var list = daily.TakeLast(3).ToList();
                string pattern = CandleStickPattern.GetPattern(list);
                if (!string.IsNullOrEmpty(pattern))
                {
                    var s = CreateScreener(symbol, StrategyEnum.RSIFULL, "D", daily.Last(), rsiDaily, rsiWeekly, rsiMonthly);
                    s.Pattern = pattern;
                    source.Add(s);
                }
            }
        }
        catch (Exception) { }
        return source;
    }

    private static List<ScreenerEntity> WealthCreationScreener(
        string symbol, List<PriceHistory> daily, 
        double rsiDaily, double rsiWeekly, double rsiMonthly)
    {
        var source = new List<ScreenerEntity>();
        try
        {
            if (daily.Count > 200)
            {
                var indicator = new WealthCreationIndicator();
                var results = indicator.Calculate(daily);
                if (results.Count > 0)
                {
                    var lastResult = results.Last();
                    if (lastResult.BuySignal)
                    {
                        var s = CreateScreener(symbol, StrategyEnum.WEALTHCREATION, "D", daily.Last(), rsiDaily, rsiWeekly, rsiMonthly);
                        source.Add(s);
                    }
                }
            }
        }
        catch (Exception) { }
        return source;
    }

    private static ScreenerEntity CreateScreener(
        string symbol, StrategyEnum strategy, string timeFrame,
        PriceHistory priceHistory, double rsiDaily, double rsiWeekly, double rsiMonthly)
    {
        return new ScreenerEntity
        {
            Symbol = symbol,
            ScreenerName = strategy.ToString(),
            TimeFrame = timeFrame,
            RecognizeDate = priceHistory.Date,
            Rsi = rsiDaily,
            RsiWeekly = rsiWeekly,
            RsiMonthly = rsiMonthly,
            Volume = (long)priceHistory.Volume,
            RecognizedPrice = priceHistory.Close
        };
    }
}
