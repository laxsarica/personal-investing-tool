namespace ScreenEdge.Backtest.Models;

public class OptimizationRow
{
    public double MonthlyThreshold { get; set; }
    public double WeeklyThreshold { get; set; }
    public double PullbackLow { get; set; }
    public double PullbackHigh { get; set; }
    public int TotalSignals { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate { get; set; }
    public double AvgReturn { get; set; }
}
