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


