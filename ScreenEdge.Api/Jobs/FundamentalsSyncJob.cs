using Hangfire;
using Microsoft.EntityFrameworkCore;
using ScreenEdge.Api.Services;
using ScreenEdge.Entity;
using ScreenEdge.Entity.Entities;

namespace ScreenEdge.Api.Jobs;

public class FundamentalsSyncJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FundamentalsSyncJob> _logger;

    public FundamentalsSyncJob(IServiceProvider serviceProvider, ILogger<FundamentalsSyncJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    public async Task SyncFundamentalsAsync()
    {
        _logger.LogInformation("Starting Yahoo Finance Fundamentals Sync Job...");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var yahooFinanceService = scope.ServiceProvider.GetRequiredService<YahooFinanceService>();

        var stocks = await context.DistinctStocks
            .Include(s => s.Fundamental)
            .OrderBy(s => s.Fundamental == null ? 0 : 1)
            .ThenBy(s => s.Id)
            .ToListAsync();

        int batchSize = 100;
        int totalStocks = stocks.Count;

        for (int i = 0; i < totalStocks; i += batchSize)
        {
            var batch = stocks.Skip(i).Take(batchSize).ToList();
            _logger.LogInformation($"Processing batch {i} to {i + batchSize} of {totalStocks}...");

            foreach (var stock in batch)
            {
                var fundamentals = await yahooFinanceService.GetFundamentalsAsync(stock.Symbol);

                if (fundamentals != null)
                {
                    if (stock.Fundamental == null)
                    {
                        stock.Fundamental = new StockFundamental { DistinctStockId = stock.Id };
                        context.StockFundamentals.Add(stock.Fundamental);
                    }

                    stock.Fundamental.PeRatio = fundamentals.PeRatio;
                    stock.Fundamental.PbRatio = fundamentals.PbRatio;
                    stock.Fundamental.DividendYield = fundamentals.DividendYield;
                    stock.Fundamental.FiftyTwoWeekHigh = fundamentals.FiftyTwoWeekHigh;
                    stock.Fundamental.FiftyTwoWeekLow = fundamentals.FiftyTwoWeekLow;

                    // Compute MarketCapCategory directly from MarketCap (which is usually in actual values, e.g. 17 Trillion for Reliance)
                    // If the value is in billions/millions, we need to convert it. Yahoo usually returns raw numbers (e.g. 17000000000000)
                    // Let's assume raw numbers and convert to Crores (1 Crore = 10,000,000)
                    if (fundamentals.MarketCap.HasValue)
                    {
                        var mCapCrores = fundamentals.MarketCap.Value / 10000000m;
                        if (mCapCrores > 20000) stock.MarketCapCategory = "LargeCap";
                        else if (mCapCrores > 5000) stock.MarketCapCategory = "MidCap";
                        else if (mCapCrores > 1000) stock.MarketCapCategory = "SmallCap";
                        else stock.MarketCapCategory = "MicroCap";
                    }
                }
            }

            await context.SaveChangesAsync();

            // Be nice to Yahoo Finance API
            if (i + batchSize < totalStocks)
            {
                _logger.LogInformation("Sleeping for 5 seconds to respect Yahoo API limits...");
                await Task.Delay(5000);
            }
        }

        _logger.LogInformation("Yahoo Finance Fundamentals Sync Job Completed Successfully.");
    }
}
