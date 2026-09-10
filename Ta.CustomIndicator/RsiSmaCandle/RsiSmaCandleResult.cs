namespace Ta.CustomIndicator.RsiSmaCandle;

/// <summary>
/// Result for a single bar from the RSI-SMA Candle (Logic 2) indicator.
/// Contains the Logic 2 ribbon OHLC, direction, slope, RSI, and buy signal.
/// </summary>
public class RsiSmaCandleResult
{
    /// <summary>Bar date.</summary>
    public DateTime Date { get; set; }

    /// <summary>Logic 2 Open — RSI-weighted SMA of SMA(Open).</summary>
    public double Logic2Open { get; set; }

    /// <summary>Logic 2 High — RSI-weighted SMA of SMA(High).</summary>
    public double Logic2High { get; set; }

    /// <summary>Logic 2 Low — RSI-weighted SMA of SMA(Low).</summary>
    public double Logic2Low { get; set; }

    /// <summary>Logic 2 Close — RSI-weighted SMA of SMA(Close).</summary>
    public double Logic2Close { get; set; }

    /// <summary>True when Logic2Close >= Logic2Open (bullish candle).</summary>
    public bool Logic2Bull { get; set; }

    /// <summary>True when Logic2Close is rising vs previous bar.</summary>
    public bool Logic2Rising { get; set; }

    /// <summary>RSI(14) value for this bar.</summary>
    public double RsiValue { get; set; }

    /// <summary>
    /// True when price crosses above Logic2High AND RSI > threshold (60).
    /// Pine Script: ta.crossover(close, logic2High) and rsiValue > 60
    /// </summary>
    public bool BuySignal { get; set; }
}
