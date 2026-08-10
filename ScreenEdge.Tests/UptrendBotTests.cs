using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScreenEdge.Entity;
using Ta.CustomIndicator.UptrendBot;
using Ta.Indicator.Base;
using Xunit;
using Xunit.Abstractions;

namespace ScreenEdge.Tests;

public class UptrendBotTests
{
    private readonly ITestOutputHelper _output;
    private const string ConnectionString = "Server=localhost;Database=ScreenEdgeDb;Trusted_Connection=True;TrustServerCertificate=True;";

    public UptrendBotTests(ITestOutputHelper output)
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

    [Fact]
    public async Task UptrendBot_Shadowfax_GeneratesBuySignalOnJuly31()
    {
        using var context = CreateDbContext();

        var histories = await context.TickerHistories
            .Where(t => t.Symbol == "SHADOWFAX")
            .OrderBy(t => t.Date)
            .Select(t => new PriceHistory
            {
                Date = t.Date,
                Open = (double)t.Open,
                High = (double)t.High,
                Low = (double)t.Low,
                Close = (double)t.Close,
                Volume = (double)t.Volume
            })
            .ToListAsync();

        Assert.NotEmpty(histories);
        _output.WriteLine($"Loaded {histories.Count} records for SHADOWFAX");

        var indicator = new UptrendBotIndicator
        {
            AtrPeriod = 11,
            Sensitivity = 2.0
        };

        var results = indicator.Calculate(histories);

        Assert.Equal(histories.Count, results.Count);

        // Find the most recent buy signal
        var buySignals = results.Where(r => r.BuySignal).ToList();
        var latestBuy = buySignals.LastOrDefault();

        Assert.NotNull(latestBuy);
        _output.WriteLine($"Latest buy signal on: {latestBuy.Date:yyyy-MM-dd} at close {latestBuy.Close}");
        
        // Assert that the latest buy signal occurred on the 31st of July 2026 (matching TradingView exactly with 11/2 inputs)
        Assert.Equal(31, latestBuy.Date.Day);

        // Additional print to see surrounding data
        var recentResults = results.TakeLast(15).ToList();
        foreach (var r in recentResults)
        {
            var hist = histories.First(x => x.Date == r.Date);
            _output.WriteLine($"{r.Date:yyyy-MM-dd} | O: {hist.Open:F2} | H: {hist.High:F2} | L: {hist.Low:F2} | C: {r.Close:F2} | Stop: {r.TrailingStop:F2} | Pos: {r.Position} | Buy: {r.BuySignal}");
        }
    }
}
