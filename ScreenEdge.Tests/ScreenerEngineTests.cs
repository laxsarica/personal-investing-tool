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
        var connectionString = "Host=db-062998dd945d.db.getvoroa.com;Port=25524;Database=pg_screener_prod;Username=postgres;Password=H2BUxvQXEhrmh71Z7syPRL3zirewITBB;SSL Mode=Require;Trust Server Certificate=true;Timeout=60;Command Timeout=120;Keepalive=30;";
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var logger = NullLogger<ScreenerEngine>.Instance;

        var engine = new ScreenerEngine(scopeFactory, logger);

        _output.WriteLine("Starting Screener Engine Job from local...");
        var result = await engine.RunScreenerJobAsync();

        _output.WriteLine($"\n=== SCREENER JOB EXECUTION COMPLETED ===");
        _output.WriteLine($"Status: {result.Status}");
        _output.WriteLine($"Time Elapsed: {result.TimeMinutes:F2} minutes ({result.TimeMinutes * 60:F1} seconds)");
        _output.WriteLine($"Total Stocks Scanned: {result.TotalStocksScanned}");
        _output.WriteLine($"Total Signals Generated: {result.RecordCount}");
        
        _output.WriteLine("\n--- Signals by Strategy ---");
        foreach(var strategy in result.SignalsByStrategy)
        {
            _output.WriteLine($"  {strategy.Key,-25}: {strategy.Value}");
        }

        if (result.Errors != null && result.Errors.Count > 0)
        {
            _output.WriteLine($"\n--- Errors Encountered ({result.Errors.Count}) ---");
            foreach (var err in result.Errors.Take(20))
            {
                _output.WriteLine($"  {err}");
            }
        }
    }

    [Fact]
    public async Task CheckScreenerNames()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var services = new ServiceCollection();
        var connectionString = "Host=db-062998dd945d.db.getvoroa.com;Port=25524;Database=pg_screener_prod;Username=postgres;Password=H2BUxvQXEhrmh71Z7syPRL3zirewITBB;SSL Mode=Require;Trust Server Certificate=true;";
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var names = await context.Screeners.Select(s => s.ScreenerName).Distinct().ToListAsync();
        _output.WriteLine("Unique Screener Names in DB: " + string.Join(", ", names));

        var latestDate = await context.Screeners.MaxAsync(s => (DateTime?)s.RecognizeDate);
        _output.WriteLine($"Latest Screener Signal Date in DB: {latestDate:yyyy-MM-dd}");

        var todaySignals = await context.Screeners
            .Where(s => s.RecognizeDate.Date == latestDate!.Value.Date)
            .GroupBy(s => s.ScreenerName)
            .Select(g => new { Strategy = g.Key, Count = g.Count() })
            .ToListAsync();

        _output.WriteLine($"Signals for {latestDate:yyyy-MM-dd}:");
        foreach(var item in todaySignals)
        {
            _output.WriteLine($"  {item.Strategy,-25}: {item.Count}");
        }
    }

    [Fact]
    public async Task VerifyStrategyConfiguration()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var services = new ServiceCollection();
        var connectionString = "Host=db-062998dd945d.db.getvoroa.com;Port=25524;Database=pg_screener_prod;Username=postgres;Password=H2BUxvQXEhrmh71Z7syPRL3zirewITBB;SSL Mode=Require;Trust Server Certificate=true;";
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var strategies = await context.ScreenerStrategies.OrderBy(s => s.Id).ToListAsync();
        
        _output.WriteLine($"Total strategies in DB: {strategies.Count}");
        _output.WriteLine("─────────────────────────────────────────────");
        foreach (var s in strategies)
        {
            _output.WriteLine($"  {s.StrategyName,-22} | {(s.IsEnabled ? "✅ ENABLED" : "❌ DISABLED"),-14} | {s.DisplayName}");
        }
        _output.WriteLine("─────────────────────────────────────────────");

        var enabled = strategies.Where(s => s.IsEnabled).Select(s => s.StrategyName).ToList();
        _output.WriteLine($"Active strategies: {string.Join(", ", enabled)}");

        Assert.Equal(9, strategies.Count);
        Assert.Contains(strategies, s => s.StrategyName == "NOLAG" && s.IsEnabled);
        Assert.Contains(strategies, s => s.StrategyName == "SUPPORTRESISTANCE" && s.IsEnabled);
        Assert.Contains(strategies, s => s.StrategyName == "RSISMACANDLE" && s.IsEnabled);
        Assert.Equal(3, strategies.Count(s => s.IsEnabled));
    }
}
