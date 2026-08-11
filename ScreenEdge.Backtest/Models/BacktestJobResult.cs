namespace ScreenEdge.Backtest.Models;

public class BacktestJobResult
{
    public DateTime RunDate { get; set; } = DateTime.Now;
    public int TotalSignals { get; set; }
    public int BacktestedSignals { get; set; }
    public int SkippedSignals { get; set; }
    public double ElapsedSeconds { get; set; }
    public Dictionary<string, StrategyStats> StrategyBreakdown { get; set; } = new();
    public List<BacktestRow> Results { get; set; } = new();
}

public class StrategyStats
{
    public int Total { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Neutral { get; set; }
    public double WinRate { get; set; }
    public double DecisiveWinRate { get; set; }
    public double AvgReturn5D { get; set; }
    public double AvgReturn10D { get; set; }
    public double AvgReturn20D { get; set; }
    public double AvgMaxDrawdown { get; set; }
    public double AvgMaxGain { get; set; }
    public double AvgDaysHeld { get; set; }
    public double AvgRealizedReturn { get; set; }
    public int Stars { get; set; }
}
