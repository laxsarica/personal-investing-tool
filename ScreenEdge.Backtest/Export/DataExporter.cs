using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ScreenEdge.Backtest.Models;

namespace ScreenEdge.Backtest.Export;

/// <summary>
/// Exports backtest results as structured JSON and CSV files for ML/LLM consumption.
/// These files are the bridge to future model training pipelines.
/// </summary>
public class DataExporter
{
    private readonly string _outputDir;
    private readonly string _datePrefix;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public DataExporter(string outputDir)
    {
        _outputDir = outputDir;
        _datePrefix = DateTime.Now.ToString("yyyy-MM-dd");
        Directory.CreateDirectory(_outputDir);
    }

    /// <summary>
    /// Export all backtest results as JSON.
    /// </summary>
    public async Task ExportJsonAsync(List<BacktestRow> results, string suffix = "backtest_data")
    {
        var path = Path.Combine(_outputDir, $"{_datePrefix}_{suffix}.json");
        var json = JsonSerializer.Serialize(results, JsonOptions);
        await File.WriteAllTextAsync(path, json);
        Console.WriteLine($"  → {path} ({results.Count} records)");
    }

    /// <summary>
    /// Export all backtest results as CSV.
    /// </summary>
    public async Task ExportCsvAsync(List<BacktestRow> results, string suffix = "backtest_data")
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("symbol,strategy,timeFrame,signalDate,entryPrice,rsiDaily,rsiWeekly,rsiMonthly,volume,pattern,return5D,return10D,return20D,return40D,maxDrawdown,maxGain,outcome");

        // Rows
        foreach (var r in results)
        {
            sb.AppendLine(
                $"{Escape(r.Symbol)},{Escape(r.StrategyName)},{Escape(r.TimeFrame)}," +
                $"{r.SignalDate:yyyy-MM-dd},{r.EntryPrice:F2},{r.RsiDaily:F2},{r.RsiWeekly:F2},{r.RsiMonthly:F2}," +
                $"{r.Volume},{Escape(r.Pattern)}," +
                $"{r.Return5D:F2},{r.Return10D:F2},{r.Return20D:F2},{r.Return40D:F2}," +
                $"{r.MaxDrawdown:F2},{r.MaxGain:F2},{r.Outcome}");
        }

        var path = Path.Combine(_outputDir, $"{_datePrefix}_{suffix}.csv");
        await File.WriteAllTextAsync(path, sb.ToString());
        Console.WriteLine($"  → {path} ({results.Count} records)");
    }

    /// <summary>
    /// Export optimization results as JSON.
    /// </summary>
    public async Task ExportOptimizationJsonAsync(List<OptimizationRow> results)
    {
        var path = Path.Combine(_outputDir, $"{_datePrefix}_optimization.json");
        var json = JsonSerializer.Serialize(results, JsonOptions);
        await File.WriteAllTextAsync(path, json);
        Console.WriteLine($"  → {path} ({results.Count} parameter sets)");
    }

    /// <summary>
    /// Escape a CSV field — wrap in quotes if it contains commas or quotes.
    /// </summary>
    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
