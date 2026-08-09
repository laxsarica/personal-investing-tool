using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScreenEdge.Api.Services;
using ScreenEdge.Entity;
using ScreenEdge.Entity.Entities;
using ScreenEdge.Repository;
using ScreenEdge.Screener;

namespace ScreenEdge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class ScreenerController : ControllerBase
{
    private readonly IScreenerEngine _screenerEngine;
    private readonly IUnitOfWorks _uow;
    private readonly DataIngestionService _dataIngestionService;
    private readonly AppDbContext _context;

    public ScreenerController(IScreenerEngine screenerEngine, IUnitOfWorks uow, DataIngestionService dataIngestionService, AppDbContext context)
    {
        _screenerEngine = screenerEngine;
        _uow = uow;
        _dataIngestionService = dataIngestionService;
        _context = context;
    }

    /// <summary>Trigger a screener job run.</summary>
    [HttpPost("run-all")]
    public async Task<IActionResult> RunAllScreeners([FromQuery] int? limit = null)
    {
        var result = await _screenerEngine.RunScreenerJobAsync(limit);
        return Ok(result);
    }

    [HttpPost("sync-data")]
    public async Task<IActionResult> SyncData()
    {
        var result = await _dataIngestionService.SyncDailyDataAsync();
        return Ok(new { Message = result });
    }

    [HttpPost("sync-historical")]
    public async Task<IActionResult> SyncHistorical([FromQuery] string symbol, [FromQuery] DateTime fromDate)
    {
        if (string.IsNullOrEmpty(symbol))
            return BadRequest("Symbol is required.");

        var result = await _dataIngestionService.SyncHistoricalDataAsync(symbol, fromDate);
        return Ok(new { Message = result });
    }

    [HttpPost("sync-all-historical")]
    public async Task<IActionResult> SyncAllHistorical([FromQuery] DateTime fromDate, [FromQuery] int? limit = null)
    {
        // This process might take hours depending on rate limits, so in a real scenario you'd use a background job tool like Hangfire.
        // For manual triggers, this will block the HTTP request until complete.
        var result = await _dataIngestionService.SyncAllHistoricalDataAsync(fromDate, limit);
        return Ok(new { Message = result });
    }

    /// <summary>Get latest screener results with optional filters.</summary>
    [HttpGet("results")]
    public async Task<IActionResult> GetResults(
        [FromQuery] string? strategy = null,
        [FromQuery] string? timeFrame = null)
    {
        var query = _context.Screeners.AsQueryable();

        // Get latest date's results
        var latestDate = await query.MaxAsync(s => (DateTime?)s.RecognizeDate);
        if (latestDate == null)
            return Ok(Array.Empty<object>());

        query = query.Where(s => s.RecognizeDate == latestDate.Value);

        if (!string.IsNullOrEmpty(strategy))
            query = query.Where(s => s.ScreenerName == strategy);

        if (!string.IsNullOrEmpty(timeFrame))
            query = query.Where(s => s.TimeFrame == timeFrame);

        var results = await query.OrderBy(s => s.Symbol).ToListAsync();
        return Ok(results);
    }

    /// <summary>Get historical screener results with date range.</summary>
    [HttpGet("results/history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? strategy = null,
        [FromQuery] string? symbol = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.Screeners.AsQueryable();

        if (from.HasValue)
            query = query.Where(s => s.RecognizeDate >= from.Value);
        if (to.HasValue)
            query = query.Where(s => s.RecognizeDate <= to.Value);
        if (!string.IsNullOrEmpty(strategy))
            query = query.Where(s => s.ScreenerName == strategy);
        if (!string.IsNullOrEmpty(symbol))
            query = query.Where(s => s.Symbol.Contains(symbol));

        var totalCount = await query.CountAsync();
        var results = await query
            .OrderByDescending(s => s.RecognizeDate)
            .ThenBy(s => s.Symbol)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { totalCount, page, pageSize, results });
    }

    /// <summary>Get screener job run history (grouped by date).</summary>
    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobs()
    {
        var jobs = await _context.Screeners
            .GroupBy(s => s.RecognizeDate.Date)
            .Select(g => new
            {
                RunDate = g.Key,
                TotalSignals = g.Count(),
                Strategies = g.GroupBy(s => s.ScreenerName)
                    .Select(sg => new { Strategy = sg.Key, Count = sg.Count() })
                    .ToList()
            })
            .OrderByDescending(j => j.RunDate)
            .Take(30)
            .ToListAsync();

        return Ok(jobs);
    }
}
