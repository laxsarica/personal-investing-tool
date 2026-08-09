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
        _logger.LogInformation("Starting daily workflow (Sync Data -> Run Screener).");
        
        try
        {
            // 1. Sync daily data
            _logger.LogInformation("Step 1: Syncing daily data...");
            await _dataIngestionService.SyncDailyDataAsync();

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
