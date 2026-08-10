using Hangfire;
using ScreenEdge.Api.Services;
using ScreenEdge.Screener;

namespace ScreenEdge.Api.Jobs;

public class ScreenerJob
{
    private readonly DataIngestionService _dataIngestionService;
    private readonly IScreenerEngine _screenerEngine;
    private readonly ILogger<ScreenerJob> _logger;

    public ScreenerJob(
        DataIngestionService dataIngestionService,
        IScreenerEngine screenerEngine,
        ILogger<ScreenerJob> logger)
    {
        _dataIngestionService = dataIngestionService;
        _screenerEngine = screenerEngine;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task RunDailyWorkflowAsync()
    {
        // Weekend guard — market is never open on Sat/Sun
        var today = DateTime.Today;
        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            _logger.LogInformation("Skipping daily workflow — {Date} ({Day}) is a weekend.",
                today, today.DayOfWeek);
            return;
        }

        _logger.LogInformation("Starting daily workflow (Sync Data -> Run Screener).");
        
        try
        {
            // 1. Sync daily data
            _logger.LogInformation("Step 1: Syncing daily data...");
            await _dataIngestionService.SyncDailyDataAsync();

            // Holiday guard — check if fresh data was actually received for today
            var latestDate = _dataIngestionService.GetLatestTickerDate();
            if (latestDate.HasValue && latestDate.Value.Date < today)
            {
                _logger.LogInformation(
                    "Skipping screener — latest ticker data is {LatestDate}, not today ({Today}). Market likely closed (holiday).",
                    latestDate.Value.Date, today);
                return;
            }

            // 2. Run screener
            _logger.LogInformation("Step 2: Running screener engine...");
            await _screenerEngine.RunScreenerJobAsync();
            
            _logger.LogInformation("Daily workflow completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during daily workflow.");
            throw; // Rethrow to let Hangfire know the job failed
        }
    }
    
    [AutomaticRetry(Attempts = 3)]
    public async Task RunDailyDataSyncAsync()
    {
        _logger.LogInformation("Starting isolated daily data sync.");
        await _dataIngestionService.SyncDailyDataAsync();
        _logger.LogInformation("Isolated daily data sync completed.");
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task RunScreenerOnlyAsync()
    {
        _logger.LogInformation("Starting isolated screener run.");
        await _screenerEngine.RunScreenerJobAsync();
        _logger.LogInformation("Isolated screener run completed.");
    }
}
