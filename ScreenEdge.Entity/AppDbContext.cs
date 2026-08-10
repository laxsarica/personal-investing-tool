using Microsoft.EntityFrameworkCore;
using ScreenEdge.Entity.Entities;

namespace ScreenEdge.Entity;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TickerHistory> TickerHistories { get; set; }
    public DbSet<DistinctStock> DistinctStocks { get; set; }
    public DbSet<Screener> Screeners { get; set; }

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

