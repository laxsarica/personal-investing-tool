namespace ScreenEdge.Screener;

public interface IScreenerEngine
{
    Task<ScreenerJobResult> RunScreenerJobAsync(int? limit = null);
}

public class ScreenerJobResult
{
    public double TimeMinutes { get; set; }
    public int RecordCount { get; set; }
    public int TotalStocksScanned { get; set; }
    public Dictionary<string, int> SignalsByStrategy { get; set; } = new();
    public string Status { get; set; } = "Completed";
    public List<string> Errors { get; set; } = new();
}
