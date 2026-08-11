using System;

namespace Ta.CustomIndicator.WealthCreation;

public class WealthCreationResult
{
    public DateTime Date { get; set; }
    public double Close { get; set; }
    public double WeeklyRsi { get; set; }
    public double Ema50 { get; set; }
    public double Ema200 { get; set; }
    public double VolMa20 { get; set; }
    public bool BuySignal { get; set; }
    public bool SellSignal { get; set; }
}
