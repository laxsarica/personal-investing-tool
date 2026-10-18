namespace ScreenEdge.Entity.Entities;

/// <summary>
/// Configurable registry of screener strategies.
/// Controls which strategies the ScreenerEngine will execute.
/// </summary>
public class ScreenerStrategy
{
    public long Id { get; set; }

    /// <summary>
    /// Strategy name matching StrategyEnum values (e.g., "NOLAG", "EMAFIFTY", "SUPPORTRESISTANCE").
    /// </summary>
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name for the strategy.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this screener strategy detects.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When true, the ScreenerEngine will execute this strategy. When false, it is skipped.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Timestamp of the last modification to this record.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
