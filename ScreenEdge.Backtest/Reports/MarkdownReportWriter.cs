using System.Text;
using ScreenEdge.Backtest.Models;

namespace ScreenEdge.Backtest.Reports;

/// <summary>
/// Generates markdown report files from backtest results.
/// All reports are written to the configured output directory with date-stamped filenames.
/// </summary>
public class MarkdownReportWriter
{
    private readonly string _outputDir; 
    private readonly string _datePrefix;

    public MarkdownReportWriter(string outputDir)
    {
        _outputDir = outputDir;
        _datePrefix = DateTime.Now.ToString("yyyy-MM-dd");
        Directory.CreateDirectory(_outputDir);
    }

    /// <summary>
    /// Write the strategy leaderboard report.
    /// </summary>
    public async Task WriteLeaderboardAsync(BacktestJobResult jobResult)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Strategy Leaderboard — {_datePrefix}");
        sb.AppendLine();
        sb.AppendLine($"> **Run Date:** {jobResult.RunDate:yyyy-MM-dd HH:mm}  ");
        sb.AppendLine($"> **Total Signals:** {jobResult.TotalSignals} | **Backtested:** {jobResult.BacktestedSignals} | **Skipped:** {jobResult.SkippedSignals}  ");
        sb.AppendLine($"> **Elapsed:** {jobResult.ElapsedSeconds:F1}s");
        sb.AppendLine();

        // Leaderboard table
        sb.AppendLine("| Rank | Strategy | Signals | Wins | Loss | Open | RawWin% | DecWin% | AvgDays | Realized% | AvgDD% | AvgGain% | Rating |");
        sb.AppendLine("|------|----------|---------|------|------|------|---------|---------|---------|-----------|--------|----------|--------|");

        var ranked = jobResult.StrategyBreakdown
            .OrderByDescending(kv => kv.Value.WinRate)
            .ToList();

        int rank = 1;
        foreach (var (strategy, stats) in ranked)
        {
            string stars = new string('★', stats.Stars) + new string('☆', 5 - stats.Stars);
            sb.AppendLine(
                $"| {rank} | {strategy} | {stats.Total} | {stats.Wins} | {stats.Losses} | {stats.Neutral} | " +
                $"{stats.WinRate:F1}% | {stats.DecisiveWinRate:F1}% | {stats.AvgDaysHeld:F1} | {stats.AvgRealizedReturn:+0.00;-0.00}% | " +
                $"{stats.AvgMaxDrawdown:F2}% | {stats.AvgMaxGain:+0.00;-0.00}% | {stars} |");
            rank++;
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("### Rating Key");
        sb.AppendLine("- ★★★★★ = Win Rate > 65%");
        sb.AppendLine("- ★★★★☆ = Win Rate > 55%");
        sb.AppendLine("- ★★★☆☆ = Win Rate > 45%");
        sb.AppendLine("- ★★☆☆☆ = Win Rate > 30%");
        sb.AppendLine("- ★☆☆☆☆ = Win Rate ≤ 30%");

        var path = Path.Combine(_outputDir, $"{_datePrefix}_leaderboard.md");
        await File.WriteAllTextAsync(path, sb.ToString());
        Console.WriteLine($"  → {path}");
    }

    /// <summary>
    /// Write a detailed strategy report with per-signal breakdown.
    /// </summary>
    public async Task WriteStrategyDetailAsync(string strategyName, BacktestJobResult jobResult)
    {
        var results = jobResult.Results
            .Where(r => r.StrategyName == strategyName)
            .ToList();

        if (results.Count == 0)
        {
            Console.WriteLine($"  → No signals for {strategyName}, skipping detail report");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"# {strategyName} Backtest — {_datePrefix}");
        sb.AppendLine();

        // Summary
        int wins = results.Count(r => r.Outcome == "Win");
        int losses = results.Count(r => r.Outcome == "Loss");
        int neutral = results.Count(r => r.Outcome == "Neutral");
        double winRate = results.Count > 0 ? (double)wins / results.Count * 100 : 0;

        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"- **Total Signals:** {results.Count}");
        sb.AppendLine($"- **Win / Loss / Neutral:** {wins} / {losses} / {neutral}");
        sb.AppendLine($"- **Win Rate:** {winRate:F1}%");
        sb.AppendLine($"- **Avg 5D Return:** {results.Average(r => r.Return5D):+0.00;-0.00}% | **Avg 10D:** {results.Average(r => r.Return10D):+0.00;-0.00}% | **Avg 20D:** {results.Average(r => r.Return20D):+0.00;-0.00}%");
        sb.AppendLine($"- **Avg Max Drawdown:** {results.Average(r => r.MaxDrawdown):F2}% | **Avg Max Gain:** {results.Average(r => r.MaxGain):+0.00;-0.00}%");
        sb.AppendLine();

        // Best & Worst
        var best = results.OrderByDescending(r => r.Return10D).FirstOrDefault();
        var worst = results.OrderBy(r => r.Return10D).FirstOrDefault();

        sb.AppendLine("## Best & Worst Signals");
        sb.AppendLine();
        sb.AppendLine("| Type | Symbol | Date | Entry Price | 10D Return |");
        sb.AppendLine("|------|--------|------|-------------|------------|");
        if (best != null)
            sb.AppendLine($"| Best | {best.Symbol} | {best.SignalDate:yyyy-MM-dd} | {best.EntryPrice:F2} | {best.Return10D:+0.00;-0.00}% |");
        if (worst != null)
            sb.AppendLine($"| Worst | {worst.Symbol} | {worst.SignalDate:yyyy-MM-dd} | {worst.EntryPrice:F2} | {worst.Return10D:+0.00;-0.00}% |");
        sb.AppendLine();

        // All Signals
        sb.AppendLine("## All Signals");
        sb.AppendLine();
        sb.AppendLine("| Date | Symbol | Entry | Exit Date | Exit Price | Days Held | Realized% | MaxGain% | MaxDD% | Outcome |");
        sb.AppendLine("|------|--------|-------|-----------|------------|-----------|-----------|----------|--------|---------|");

        foreach (var r in results.OrderBy(r => r.SignalDate))
        {
            string outcome = r.Outcome;
            string exitDate = r.ExitDate.HasValue ? r.ExitDate.Value.ToString("yyyy-MM-dd") : "-";
            string exitPrice = r.ExitPrice.HasValue ? r.ExitPrice.Value.ToString("F2") : "-";
            string daysHeld = r.DaysHeld.HasValue ? r.DaysHeld.Value.ToString() : "-";
            string realized = r.RealizedReturn.HasValue ? $"{r.RealizedReturn.Value:+0.00;-0.00}%" : "-";

            sb.AppendLine(
                $"| {r.SignalDate:yyyy-MM-dd} | {r.Symbol} | {r.EntryPrice:F2} | {exitDate} | {exitPrice} | {daysHeld} | " +
                $"{realized} | {r.MaxGain:F2}% | {r.MaxDrawdown:F2}% | **{outcome}** |");
        }

        var path = Path.Combine(_outputDir, $"{_datePrefix}_{strategyName}_detail.md");
        await File.WriteAllTextAsync(path, sb.ToString());
        Console.WriteLine($"  → {path}");
    }

    /// <summary>
    /// Write grid search optimization results.
    /// </summary>
    public async Task WriteOptimizationAsync(string strategyName, List<OptimizationRow> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {strategyName} Parameter Optimization — {_datePrefix}");
        sb.AppendLine();
        sb.AppendLine($"> **RSI Period:** 14 (fixed)  ");
        sb.AppendLine($"> **Parameter Sets Evaluated:** {results.Count}  ");
        sb.AppendLine($"> **Win Threshold:** +5.0% | **Loss Threshold:** -3.0%");
        sb.AppendLine();

        sb.AppendLine("## Top Parameter Sets");
        sb.AppendLine();
        sb.AppendLine("| Rank | Monthly> | Weekly> | Pullback Zone | Signals | Wins | Losses | RawWin% | DecWin% | AvgReturn% |");
        sb.AppendLine("|------|----------|---------|---------------|---------|------|--------|---------|---------|------------|");

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            sb.AppendLine(
                $"| {i + 1} | {r.MonthlyThreshold:F0} | {r.WeeklyThreshold:F0} | " +
                $"{r.PullbackLow:F0}–{r.PullbackHigh:F0} | " +
                $"{r.TotalSignals} | {r.Wins} | {r.Losses} | {r.WinRate:F1}% | {r.DecisiveWinRate:F1}% | {r.AvgReturn:+0.00;-0.00}% |");
        }

        if (results.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Recommended Parameters");
            sb.AppendLine();
            var top = results[0];
            sb.AppendLine($"- **Monthly RSI >** {top.MonthlyThreshold:F0}");
            sb.AppendLine($"- **Weekly RSI >** {top.WeeklyThreshold:F0}");
            sb.AppendLine($"- **Daily RSI Pullback Zone:** {top.PullbackLow:F0}–{top.PullbackHigh:F0}");
            sb.AppendLine($"- **Win Rate:** {top.WinRate:F1}% across {top.TotalSignals} signals");
            sb.AppendLine($"- **Avg 10D Return:** {top.AvgReturn:+0.00;-0.00}%");
        }

        var path = Path.Combine(_outputDir, $"{_datePrefix}_{strategyName}_optimize.md");
        await File.WriteAllTextAsync(path, sb.ToString());
        Console.WriteLine($"  → {path}");
    }
}
