namespace ScreenEdge.Entity.Entities;

public class StockFundamental
{
    public long Id { get; set; }
    
    // Foreign key to DistinctStock
    public long DistinctStockId { get; set; }
    public DistinctStock DistinctStock { get; set; } = null!;

    // Fundamentals
    public decimal? PeRatio { get; set; }
    public decimal? PbRatio { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? FiftyTwoWeekHigh { get; set; }
    public decimal? FiftyTwoWeekLow { get; set; }
    
    // Profile
    public string Industry { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DateTime LastUpdated { get; set; }
}
