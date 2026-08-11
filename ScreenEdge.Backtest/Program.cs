using ScreenEdge.Backtest;
using ScreenEdge.Backtest.Export;
using ScreenEdge.Backtest.Models;
using ScreenEdge.Backtest.Reports;
using ScreenEdge.AI;

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

    case "ai-train":
        RunAiTrain();
        break;

    case "ai-test":
        RunAiTest();
        break;

    default:
        Console.WriteLine("Commands:");
        Console.WriteLine("  run       — Scan ALL stocks through full history and backtest RSITTF");
        Console.WriteLine("  optimize  — Grid search RSITTF thresholds over full history");
        Console.WriteLine("  wealth    — Backtest the Wealth Creation strategy (Weekly RSI 60 cross, EMA50 exit)");
        Console.WriteLine("  ai-train  — Train ML.NET model on wealth creation backtest data");
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

void RunAiTrain()
{
    Console.WriteLine("\n[AI Train] Training ML.NET Model...\n");
    string csvPath = Path.Combine(outputDir, "2026-08-11_historical_wealth_data.csv");
    ModelBuilder.TrainModel(csvPath);
}

void RunAiTest()
{
    Console.WriteLine("\n[AI Test] Predicting recent signals (July 31 and Aug 7)...\n");
    string csvPath = Path.Combine(outputDir, "2026-08-11_historical_wealth_data.csv");
    
    using var fs = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    using var sr = new StreamReader(fs);
    string header = sr.ReadLine();
    
    Console.WriteLine($"{"Symbol",-15} | {"Date",-12} | {"Outcome",-10} | {"AI Win Prob",-15} | {"Action"}");
    Console.WriteLine(new string('-', 75));
    
    string line;
    while ((line = sr.ReadLine()) != null)
    {
        var cols = line.Split(',');
        if (cols.Length < 17) continue;

        string signalDate = cols[3];
        if (signalDate.Contains("2026-07-31") || signalDate.Contains("2026-08-07"))
        {
            var input = new ScreenerModelInput
            {
                Symbol = cols[0],
                Strategy = cols[1],
                TimeFrame = cols[2],
                SignalDate = cols[3],
                EntryPrice = float.TryParse(cols[4], out var ep) ? ep : 0,
                RsiDaily = float.TryParse(cols[5], out var rd) ? rd : 0,
                RsiWeekly = float.TryParse(cols[6], out var rw) ? rw : 0,
                RsiMonthly = float.TryParse(cols[7], out var rm) ? rm : 0,
                Volume = float.TryParse(cols[8], out var v) ? v : 0,
            };

            var prediction = ModelBuilder.Predict(input);
            double prob = Math.Round(prediction.Probability * 100.0, 2);
            string outcome = cols[16];
            
            // Suggest action: if probability > 50%, Buy. Otherwise Skip.
            string action = prob > 50.0 ? "BUY" : "SKIP";
            
            Console.WriteLine($"{input.Symbol,-15} | {input.SignalDate,-12} | {outcome,-10} | {prob,6}%        | {action}");
        }
    }
}
