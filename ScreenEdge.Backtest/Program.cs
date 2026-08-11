using ScreenEdge.Backtest;
using ScreenEdge.Backtest.Export;
using ScreenEdge.Backtest.Models;
using ScreenEdge.Backtest.Reports;

const string connectionString = "Server=localhost;Database=ScreenEdgeDb;Trusted_Connection=True;TrustServerCertificate=True;";
var outputDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Data", "Backtest"));

Console.WriteLine($"ScreenEdge Backtest Engine (Historical Scan Mode)");
Console.WriteLine($"Output: {outputDir}");
Console.WriteLine(new string('=', 80));

string command = args.Length > 0 ? args[0].ToLower() : "run";

var gridSearchEngine = new GridSearchEngine(connectionString);
var historicalEngine = new HistoricalBacktestEngine(connectionString);
var wealthEngine = new WealthCreationBacktestEngine(connectionString);
var reportWriter = new MarkdownReportWriter(outputDir);
var dataExporter = new DataExporter(outputDir);

switch (command)
{
    case "run":
        await RunHistorical();
        break;

    case "optimize":
        await RunOptimize();
        break;

    case "wealth":
        await RunWealth();
        break;

    case "all":
        await RunHistorical();
        await RunOptimize();
        break;

    default:
        Console.WriteLine("Commands:");
        Console.WriteLine("  run       — Scan ALL stocks through full history and backtest RSITTF");
        Console.WriteLine("  optimize  — Grid search RSITTF thresholds over full history");
        Console.WriteLine("  wealth    — Backtest the Wealth Creation strategy (Weekly RSI 60 cross, EMA50 exit)");
        Console.WriteLine("  all       — Run both");
        break;
}

Console.WriteLine();
Console.WriteLine("Done.");

// ──────────────────────────────────────────────────────
// Command implementations
// ──────────────────────────────────────────────────────

async Task RunHistorical()
{
    Console.WriteLine("\n[Run] Scanning ALL stocks through full price history...\n");

    var result = await historicalEngine.RunHistoricalRsiTtfAsync();

    Console.WriteLine("\nWriting reports...");
    await reportWriter.WriteLeaderboardAsync(result);
    await reportWriter.WriteStrategyDetailAsync("RSITTF", result);
    await dataExporter.ExportJsonAsync(result.Results, "historical_rsittf_data");
    await dataExporter.ExportCsvAsync(result.Results, "historical_rsittf_data");

    Console.WriteLine($"\n  Total signals: {result.TotalSignals}");
    if (result.StrategyBreakdown.TryGetValue("RSITTF", out var stats))
    {
        Console.WriteLine($"  Win: {stats.Wins} | Loss: {stats.Losses} | Neutral: {stats.Neutral}");
        Console.WriteLine($"  Win Rate: {stats.WinRate:F1}%");
        Console.WriteLine($"  Avg 10D Return: {stats.AvgReturn10D:+0.00;-0.00}%");
    }
}

async Task RunOptimize()
{
    Console.WriteLine("\n[Optimize] Running historical grid search...\n");
    
    var parameters = new GridSearchParameters();
    var optimizationResults = await gridSearchEngine.RunGridSearchAsync(parameters);

    Console.WriteLine("\nWriting reports...");
    await reportWriter.WriteOptimizationAsync("RSITTF", optimizationResults);
    await dataExporter.ExportOptimizationJsonAsync(optimizationResults);
}

async Task RunWealth()
{
    Console.WriteLine("\n[Wealth] Running Wealth Creation strategy backtest...\n");

    var result = await wealthEngine.RunAsync();

    Console.WriteLine("\nWriting reports...");
    await reportWriter.WriteLeaderboardAsync(result);
    await reportWriter.WriteStrategyDetailAsync("WealthCreation", result);
    await dataExporter.ExportJsonAsync(result.Results, "historical_wealth_data");
    await dataExporter.ExportCsvAsync(result.Results, "historical_wealth_data");

    Console.WriteLine($"\n  Total signals: {result.TotalSignals}");
    if (result.StrategyBreakdown.TryGetValue("WealthCreation", out var stats))
    {
        Console.WriteLine($"  Win: {stats.Wins} | Loss: {stats.Losses} | Neutral: {stats.Neutral}");
        Console.WriteLine($"  Win Rate: {stats.WinRate:F1}%");
        Console.WriteLine($"  Avg Days Held: {stats.AvgDaysHeld:F1}");
        Console.WriteLine($"  Avg Realized Return: {stats.AvgRealizedReturn:+0.00;-0.00}%");
    }
}
