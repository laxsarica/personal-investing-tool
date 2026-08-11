using ScreenEdge.Backtest;
using ScreenEdge.Backtest.Export;
using ScreenEdge.Backtest.Models;
using ScreenEdge.Backtest.Reports;

const string connectionString = "Server=localhost;Database=ScreenEdgeDb;Trusted_Connection=True;TrustServerCertificate=True;";
var outputDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Data", "Backtest"));

Console.WriteLine($"ScreenEdge Backtest Engine");
Console.WriteLine($"Output: {outputDir}");
Console.WriteLine(new string('=', 80));

string command = args.Length > 0 ? args[0].ToLower() : "all";
string? strategyArg = args.Length > 1 ? args[1].ToUpper() : null;

var backtestEngine = new BacktestEngine(connectionString);
var gridSearchEngine = new GridSearchEngine(connectionString);
var historicalEngine = new HistoricalBacktestEngine(connectionString);
var reportWriter = new MarkdownReportWriter(outputDir);
var dataExporter = new DataExporter(outputDir);

switch (command)
{
    case "leaderboard":
        await RunLeaderboard();
        break;

    case "strategy":
        if (strategyArg == null)
        {
            Console.WriteLine("Usage: dotnet run -- strategy <STRATEGY_NAME>");
            Console.WriteLine("  e.g. dotnet run -- strategy RSITTF");
            return;
        }
        await RunStrategyDetail(strategyArg);
        break;

    case "optimize":
        await RunOptimize();
        break;

    case "historical":
        await RunHistorical();
        break;

    case "all":
        await RunLeaderboard();
        await RunOptimize();
        // Write detail reports for each strategy found
        var allResult = await backtestEngine.RunLeaderboardAsync();
        foreach (var strategy in allResult.StrategyBreakdown.Keys)
        {
            await reportWriter.WriteStrategyDetailAsync(strategy, allResult);
        }
        break;

    default:
        Console.WriteLine("Commands:");
        Console.WriteLine("  leaderboard     — Backtest all strategies from Screeners table");
        Console.WriteLine("  strategy <NAME> — Detailed backtest for one strategy");
        Console.WriteLine("  optimize        — Grid search RSITTF thresholds");
        Console.WriteLine("  historical      — Scan ALL stocks through full history (no Screeners table needed)");
        Console.WriteLine("  all             — Run leaderboard + optimize + per-strategy details");
        break;
}

Console.WriteLine();
Console.WriteLine("Done.");

// ──────────────────────────────────────────────────────
// Command implementations
// ──────────────────────────────────────────────────────

async Task RunLeaderboard()
{
    Console.WriteLine("\n[Leaderboard] Running backtest for all strategies...\n");
    var result = await backtestEngine.RunLeaderboardAsync();

    Console.WriteLine("\nWriting reports...");
    await reportWriter.WriteLeaderboardAsync(result);
    await dataExporter.ExportJsonAsync(result.Results);
    await dataExporter.ExportCsvAsync(result.Results);
}

async Task RunStrategyDetail(string strategyName)
{
    Console.WriteLine($"\n[Strategy Detail] Running backtest for {strategyName}...\n");
    var result = await backtestEngine.RunStrategyDetailAsync(strategyName);

    Console.WriteLine("\nWriting reports...");
    await reportWriter.WriteStrategyDetailAsync(strategyName, result);
    await dataExporter.ExportJsonAsync(result.Results, $"{strategyName.ToLower()}_data");
    await dataExporter.ExportCsvAsync(result.Results, $"{strategyName.ToLower()}_data");
}

async Task RunOptimize()
{
    Console.WriteLine("\n[Optimize] Running RSITTF grid search...\n");
    var parameters = new GridSearchParameters();
    var optimizationResults = await gridSearchEngine.RunGridSearchAsync(parameters);

    Console.WriteLine("\nWriting reports...");
    await reportWriter.WriteOptimizationAsync("RSITTF", optimizationResults);
    await dataExporter.ExportOptimizationJsonAsync(optimizationResults);
}

async Task RunHistorical()
{
    Console.WriteLine("\n[Historical] Scanning ALL stocks through full price history...\n");
    Console.WriteLine("This does NOT use the Screeners table — it finds signals from raw TickerHistory data.\n");

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
