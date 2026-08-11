namespace ScreenEdge.Backtest.Models;

public class BacktestRow
{
    public long ScreenerId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string StrategyName { get; set; } = string.Empty;
    public string TimeFrame { get; set; } = string.Empty;
    public DateTime SignalDate { get; set; }
    public double EntryPrice { get; set; }
    public double RsiDaily { get; set; }
    public double RsiWeekly { get; set; }
    public double RsiMonthly { get; set; }
    public long Volume { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public double Return5D { get; set; }
    public double Return10D { get; set; }
    public double Return20D { get; set; }
    public double Return40D { get; set; }
    public double MaxDrawdown { get; set; }
    public double MaxGain { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public DateTime? ExitDate { get; set; }
    public double? ExitPrice { get; set; }
    public int? DaysHeld { get; set; }
    public double? RealizedReturn { get; set; }
}
