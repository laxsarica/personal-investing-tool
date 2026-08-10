using System;

namespace Ta.CustomIndicator.UptrendBot;

public class UptrendBotResult
{
    public DateTime Date { get; set; }
    public double Close { get; set; }
    public double TrailingStop { get; set; }
    public int Position { get; set; }
    public bool BuySignal { get; set; }
    public bool SellSignal { get; set; }
}
