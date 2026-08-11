namespace ScreenEdge.Entity.Entities;

public class Screener
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
    public string Pattern { get; set; } = string.Empty;
    public double? AiScore { get; set; }
}
