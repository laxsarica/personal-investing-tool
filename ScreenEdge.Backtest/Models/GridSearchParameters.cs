namespace ScreenEdge.Backtest.Models;

public class GridSearchParameters
{
    public double[] MonthlyThresholds { get; set; } = [50, 55, 60, 65, 70];
    public double[] WeeklyThresholds { get; set; } = [50, 55, 60, 65, 70];
    public double[] PullbackLows { get; set; } = [30, 35, 40];
    public double PullbackBandWidth { get; set; } = 10.0;

    /// <summary>
    /// Win threshold — forward return >= this % counts as a Win.
    /// </summary>
    public double WinThresholdPercent { get; set; } = 5.0;

    /// <summary>
    /// Loss threshold — forward return &lt;= this % counts as a Loss.
    /// </summary>
    public double LossThresholdPercent { get; set; } = -3.0;

    /// <summary>
    /// Minimum number of signals for a parameter set to be included in results.
    /// </summary>
    public int MinSignals { get; set; } = 3;

    /// <summary>
    /// Number of top parameter sets to include in the report.
    /// </summary>
    public int TopN { get; set; } = 1000;
}
