---
name: screener-automation
description: Skill for automating the screener system — scheduling screener jobs, background task execution, API-triggered runs, and result persistence workflows. Covers Hangfire/Quartz.NET scheduling, parallel execution patterns, and the end-to-end automation pipeline from data fetch to signal detection to storage.
---

# Screener Automation Skill

## Overview
This skill covers how to automate the technical screener system so it runs on schedule without manual intervention. It documents the full pipeline: triggering → data loading → parallel screening → result persistence → notification.

## Automation Pipeline

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────────┐
│  Trigger         │────▶│  Load Stock List  │────▶│  Parallel Screening │
│  (Schedule/API)  │     │  + Price History   │     │  (10 concurrent)    │
└─────────────────┘     └──────────────────┘     └─────────┬───────────┘
                                                           │
                    ┌──────────────────┐     ┌─────────────▼───────────┐
                    │  Return Results   │◀───│  Filter + Persist to DB │
                    │  + Notifications   │     │  (RSI 55-70 band)      │
                    └──────────────────┘     └─────────────────────────┘
```

## Trigger Mechanisms

### 1. API Endpoint Trigger
```csharp
// Controller
[HttpPost("run")]
public async Task<IActionResult> RunScreener([FromServices] IScreenerEngine engine)
{
    var result = await engine.RunScreenerJob();
    return Ok(result);
}
```

### 2. Scheduled Background Job (Recommended: Hangfire)
```csharp
// In Program.cs
builder.Services.AddHangfire(config => config.UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();

// Register recurring job (runs at 6:30 PM IST on weekdays — after market close)
RecurringJob.AddOrUpdate<IScreenerEngine>(
    "daily-screener",
    engine => engine.RunScreenerJob(),
    "0 18 30 ? * MON-FRI",     // Cron: 6:30 PM, Mon-Fri
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time") }
);
```

### 3. Alternative: Quartz.NET
```csharp
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("ScreenerJob");
    q.AddJob<ScreenerJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithCronSchedule("0 30 18 ? * MON-FRI"));
});
builder.Services.AddQuartzHostedService();
```

## Parallel Execution Best Practices

### Thread-Safe Result Collection
```csharp
// WRONG (original code) - List<T> is not thread-safe
List<Screener> screeners = new List<Screener>();
Parallel.ForEach(stocks, stock => screeners.AddRange(results));

// CORRECT - Use ConcurrentBag<T>
ConcurrentBag<Screener> screeners = new ConcurrentBag<Screener>();
Parallel.ForEach(stocks, stock => {
    foreach (var r in results) screeners.Add(r);
});
```

### Scoped DbContext in Parallel
```csharp
// WRONG - Creating DbContext directly
using (var context = new AppDbContext()) { ... }

// CORRECT - Use IServiceScopeFactory
Parallel.ForEach(stocks, stock => {
    using var scope = serviceScopeFactory.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // ... use context ...
});
```

### Instance State Safety
```csharp
// WRONG - Shared mutable state in parallel loop
private double RsiDaily;  // Race condition!

// CORRECT - Thread-local variables
Parallel.ForEach(stocks, stock => {
    double rsiDaily = GetRsi(dailyData);      // local variable
    double rsiWeekly = GetRsi(weeklyData);     // local variable
    // ... use local values ...
});
```

## Result Persistence Strategy

### Bulk Insert Pattern
```csharp
public async Task PersistScreenerResults(IEnumerable<Screener> results)
{
    var validResults = results.Where(x => x != null).ToList();
    
    // Tag with run timestamp
    var runId = DateTime.UtcNow;
    foreach (var r in validResults)
        r.RunTimestamp = runId;
    
    await using var context = dbContextFactory.CreateDbContext();
    context.Screeners.AddRange(validResults);
    await context.SaveChangesAsync();
}
```

### Historical Result Management
- Keep results from last N runs (configurable, default: 30 days)
- Add a cleanup job to purge old results
- Consider adding a `RunId` (GUID) column to group results per execution

## Monitoring & Notifications

### Job Status Tracking
```csharp
public class ScreenerJobResult
{
    public Guid RunId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int TotalStocksScanned { get; set; }
    public int SignalsDetected { get; set; }
    public Dictionary<string, int> SignalsByStrategy { get; set; }
    public string Status { get; set; }  // "Completed", "Failed", "PartialSuccess"
    public List<string> Errors { get; set; }
}
```

### Recommended Notification Channels
1. **API Response** — For manual triggers
2. **SignalR WebSocket** — Push real-time results to connected dashboard clients
3. **Webhook** — POST results to external integrations (Telegram bot, email service)

## Scheduling Considerations for Indian Market
- **Market hours:** 9:15 AM - 3:30 PM IST (Mon-Fri)
- **Best run time:** 6:00 PM - 7:00 PM IST (after data providers update)
- **Holiday awareness:** NSE publishes holiday calendar — skip runs on holidays
- **Pre-market scan:** Optional run at 8:30 AM for pre-market analysis
- **Weekend batch:** Optional Saturday run for weekly timeframe analysis

## Configuration
```json
{
  "Screener": {
    "MaxParallelism": 10,
    "RsiMinimum": 55.0,
    "RsiMaximum": 70.0,
    "MinimumBarsDaily": 30,
    "MinimumBarsForSR": 20,
    "MinimumBarsForRsiWma": 55,
    "ScheduleCron": "0 30 18 ? * MON-FRI",
    "TimeZone": "India Standard Time",
    "RetainResultsDays": 30
  }
}
```
