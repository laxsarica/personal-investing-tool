namespace ScreenEdge.Api.Models;

public class ScreenerResultDto
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string ScreenerName { get; set; } = string.Empty;
    public string TimeFrame { get; set; } = string.Empty;
    public DateTime RecognizeDate { get; set; }
    public double Rsi { get; set; }
    public double RsiWeekly { get; set; }
    public double RsiMonthly { get; set; }
    public long Volume { get; set; }
    public double RecognizedPrice { get; set; }
    
    // New property from DistinctStock
    public string? MarketCapCategory { get; set; }
}
