using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ScreenEdge.Entity;
using ScreenEdge.Entity.Entities;
using ScreenEdge.Screener;
using Xunit;

namespace ScreenEdge.Tests;
using Xunit.Abstractions;

public class ScreenerEngineTests
{
    private readonly ITestOutputHelper _output;

    public ScreenerEngineTests(ITestOutputHelper output)
    {
        _output = output;
    }
    private IServiceScopeFactory CreateMockScopeFactory(AppDbContext dbContext)
    {
        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task ScreenerEngine_RunsSuccessfully_WithNoData()
    {
        // Setup in-memory DB
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "EmptyDb")
            .Options;
            
        using var context = new AppDbContext(options);
        var logger = new NullLogger<ScreenerEngine>();
        var engine = new ScreenerEngine(CreateMockScopeFactory(context), logger);

        var result = await engine.RunScreenerJobAsync();

        Assert.Equal("Completed", result.Status);
        Assert.Equal(0, result.TotalStocksScanned);
        Assert.Equal(0, result.RecordCount);
    }

    [Fact]
    public async Task ScreenerEngine_HandlesInsufficientDataGracefully()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "InsufficientDataDb")
            .Options;
            
        using var context = new AppDbContext(options);
        
        // Add distinct stock
        context.DistinctStocks.Add(new DistinctStock { Symbol = "TEST", Exchange = "NSE", TotalTradingDays = 25 });
        
        // Add only 10 days of history (engine needs > 30)
        var date = new DateTime(2025, 1, 1);
        for(int i = 0; i < 10; i++)
        {
            context.TickerHistories.Add(new TickerHistory 
            { 
                Symbol = "TEST", 
                Date = date.AddDays(i),
                Open = 100, High = 105, Low = 95, Close = 102, Volume = 10000 
            });
        }
        await context.SaveChangesAsync();

        var logger = new NullLogger<ScreenerEngine>();
        var engine = new ScreenerEngine(CreateMockScopeFactory(context), logger);

        var result = await engine.RunScreenerJobAsync();

        Assert.Equal("Completed", result.Status);
        Assert.Equal(1, result.TotalStocksScanned);
        Assert.Equal(0, result.RecordCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task TestScreenerLive()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var services = new ServiceCollection();
        var connectionString = "Host=db.getvoroa.com;Port=25524;Database=pg_screener_prod;Username=postgres;Password=gKzmiSPjxLeswnZtbYvVthRag37zvZ52;SSL Mode=Require;Trust Server Certificate=true;";
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var logger = NullLogger<ScreenerEngine>.Instance;

        var engine = new ScreenerEngine(scopeFactory, logger);

        _output.WriteLine("Starting Screener Engine Job...");
        var result = await engine.RunScreenerJobAsync();

        _output.WriteLine($"Screener completed in {result.TimeMinutes:F2} minutes.");
        _output.WriteLine($"Total Stocks Scanned: {result.TotalStocksScanned}");
        _output.WriteLine($"Total Signals Generated: {result.RecordCount}");
        
        foreach(var strategy in result.SignalsByStrategy)
        {
            _output.WriteLine($"- {strategy.Key}: {strategy.Value}");
        }
    }

    [Fact]
    public async Task CleanUpRsiFull()
    {
        var services = new ServiceCollection();
        var connectionString = "Host=db.getvoroa.com;Port=25524;Database=pg_screener_prod;Username=postgres;Password=gKzmiSPjxLeswnZtbYvVthRag37zvZ52;SSL Mode=Require;Trust Server Certificate=true;";
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var oldRsiFull = await context.Screeners.Where(x => x.ScreenerName == "RSIFULL").ToListAsync();
        _output.WriteLine($"Found {oldRsiFull.Count} old RSIFULL records.");
        
        if (oldRsiFull.Count > 0)
        {
            context.Screeners.RemoveRange(oldRsiFull);
            await context.SaveChangesAsync();
            _output.WriteLine("Deleted all old RSIFULL records.");
        }
    }
}
