namespace ScreenEdge.Entity.Entities;

public class DistinctStock
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Exchange { get; set; } = "NSE";
    public int TotalTradingDays { get; set; }
    
    public string? MarketCapCategory { get; set; }
    public StockFundamental? Fundamental { get; set; }
}
