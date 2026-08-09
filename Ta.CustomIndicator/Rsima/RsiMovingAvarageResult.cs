using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ta.CustomIndicator.RsiWma;

public class RsiWmaResult
{
    /// <summary>Bar date/time.</summary>
    public DateTime Date { get; set; }

    /// <summary>RSI value for this bar (0–100).</summary>
    public double RsiValue { get; set; }

    /// <summary>RSI-Weighted Moving Average value.</summary>
    public double RsiWma { get; set; }

    /// <summary>Simple Moving Average value (NaN if ShowSma is false).</summary>
    public double Sma { get; set; }

    /// <summary>Percentage deviation of RSI-WMA from SMA: (RsiWma - Sma) / Sma * 100.</summary>
    public double DeviationFromSma { get; set; }

    /// <summary>True when price crosses above the RSI-WMA (bullish signal).</summary>
    public bool BullishCross { get; set; }

    /// <summary>True when price crosses below the RSI-WMA (bearish signal).</summary>
    public bool BearishCross { get; set; }
}
