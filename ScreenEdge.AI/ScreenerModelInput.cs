using Microsoft.ML.Data;

namespace ScreenEdge.AI;

public class ScreenerModelInput
{
    [LoadColumn(0)] public string? Symbol { get; set; }
    [LoadColumn(1)] public string? Strategy { get; set; }
    [LoadColumn(2)] public string? TimeFrame { get; set; }
    [LoadColumn(3)] public string? SignalDate { get; set; }
    [LoadColumn(4)] public float EntryPrice { get; set; }
    [LoadColumn(5)] public float RsiDaily { get; set; }
    [LoadColumn(6)] public float RsiWeekly { get; set; }
    [LoadColumn(7)] public float RsiMonthly { get; set; }
    [LoadColumn(8)] public float Volume { get; set; }
    [LoadColumn(9)] public string? Pattern { get; set; }
    [LoadColumn(10)] public float Return5D { get; set; }
    [LoadColumn(11)] public float Return10D { get; set; }
    [LoadColumn(12)] public float Return20D { get; set; }
    [LoadColumn(13)] public float Return40D { get; set; }
    [LoadColumn(14)] public float MaxDrawdown { get; set; }
    [LoadColumn(15)] public float MaxGain { get; set; }
    [LoadColumn(16)] public string? Outcome { get; set; } // "Win", "Loss", "Neutral"

    // The label we want to predict (true = Win, false = Loss). We'll map this during the pipeline.
    public bool IsWin { get; set; }
}
