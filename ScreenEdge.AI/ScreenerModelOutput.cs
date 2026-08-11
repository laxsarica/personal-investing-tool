using Microsoft.ML.Data;

namespace ScreenEdge.AI;

public class ScreenerModelOutput
{
    [ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }
}
