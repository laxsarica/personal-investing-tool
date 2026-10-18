using Microsoft.EntityFrameworkCore;
using ScreenEdge.Entity.Entities;

namespace ScreenEdge.Entity;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TickerHistory> TickerHistories { get; set; }
    public DbSet<DistinctStock> DistinctStocks { get; set; }
    public DbSet<StockFundamental> StockFundamentals { get; set; }
    public DbSet<Screener> Screeners { get; set; }
    public DbSet<ScreenerStrategy> ScreenerStrategies { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(new PostgreSqlReadWriteInterceptor());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TickerHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Symbol);
            entity.HasIndex(e => new { e.Symbol, e.Date }).IsUnique();
        });

        modelBuilder.Entity<DistinctStock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Symbol).IsUnique();
            
            entity.HasOne(d => d.Fundamental)
                  .WithOne(p => p.DistinctStock)
                  .HasForeignKey<StockFundamental>(d => d.DistinctStockId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Screener>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Symbol);
            entity.HasIndex(e => e.ScreenerName);
            entity.HasIndex(e => e.RecognizeDate);
            entity.HasIndex(e => new { e.Symbol, e.ScreenerName, e.TimeFrame, e.RecognizeDate })
                  .IsUnique();
        });

        modelBuilder.Entity<ScreenerStrategy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StrategyName).IsUnique();

            // Seed all strategies — only RSISMACANDLE, NOLAG, SUPPORTRESISTANCE enabled
            entity.HasData(
                new ScreenerStrategy { Id = 1, StrategyName = "NOLAG", DisplayName = "ZeroLag", Description = "ZeroLag crossover screener — detects momentum shift using zero-lag EMA signals.", IsEnabled = true, UpdatedAt = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc) },
                new ScreenerStrategy { Id = 2, StrategyName = "EMAFIFTY", DisplayName = "EMA 50", Description = "EMA-50 crossover screener — detects price crossing above EMA-50.", IsEnabled = false, UpdatedAt = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc) },
                new ScreenerStrategy { Id = 3, StrategyName = "SUPPORTRESISTANCE", DisplayName = "Support/Resistance Breakout", Description = "Support/Resistance breakout screener — detects price breaking out above key resistance levels.", IsEnabled = true, UpdatedAt = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc) },
                new ScreenerStrategy { Id = 4, StrategyName = "RSIWMA", DisplayName = "RSI Weighted MA", Description = "RSI-Weighted Moving Average screener — detects bullish RSI-WMA crossovers.", IsEnabled = false, UpdatedAt = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc) },
                new ScreenerStrategy { Id = 5, StrategyName = "RSITTF", DisplayName = "RSI Triple Timeframe", Description = "RSI Triple Timeframe — monthly/weekly uptrend with daily pullback recovery.", IsEnabled = false, UpdatedAt = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc) },
                new ScreenerStrategy { Id = 6, StrategyName = "RSIFULL", DisplayName = "RSI Full + Candlestick", Description = "RSI oversold zone with bullish candlestick pattern confirmation.", IsEnabled = false, UpdatedAt = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc) },
                new ScreenerStrategy { Id = 7, StrategyName = "UPTRENDBOT", DisplayName = "Uptrend Bot", Description = "ATR-based uptrend detection bot — identifies buy signals from trend reversals.", IsEnabled = false, UpdatedAt = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc) },
                new ScreenerStrategy { Id = 8, StrategyName = "WEALTHCREATION", DisplayName = "Wealth Creation", Description = "Long-term wealth creation indicator — identifies stocks in sustained uptrends.", IsEnabled = false, UpdatedAt = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc) },
                new ScreenerStrategy { Id = 9, StrategyName = "RSISMACANDLE", DisplayName = "RSI SMA Candle", Description = "RSI + SMA + Candlestick composite screener — detects buy signals from multi-indicator confluence.", IsEnabled = true, UpdatedAt = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc) }
            );
        });
    }
}

public class PostgreSqlReadWriteInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.DbConnectionInterceptor
{
    public override void ConnectionOpened(System.Data.Common.DbConnection connection, Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SET default_transaction_read_only = off; SET transaction_read_only = off;";
        cmd.ExecuteNonQuery();
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(System.Data.Common.DbConnection connection, Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SET default_transaction_read_only = off; SET transaction_read_only = off;";
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }
}


