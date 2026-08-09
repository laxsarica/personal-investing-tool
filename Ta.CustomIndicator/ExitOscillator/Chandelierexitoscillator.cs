using Ta.Indicator.Base;
using TA.Indicators.Indicator;

namespace Ta.CustomIndicator.ChandelierExit;

public class ChandelierExitResult
{
    public DateTime Date { get; set; }
    public int Direction { get; set; }   // 1 = Bullish, -1 = Bearish
    public double ExitLevel { get; set; }   // Trailing stop price level
    public double Oscillator { get; set; }   // Normalized 0-100
    public bool DirectionChanged { get; set; }   // True on trend flip bar
}
public class ChandelierExitOscillator
{
    public int AtrLength { get; set; } = 22;
    public double Multiplier { get; set; } = 3.0;
    public int Smoothing { get; set; } = 3;

    public List<double> Prices { get; set; } = new List<double>();

    public List<ChandelierExitResult> Calculate(List<PriceHistory> priceHistories)
    {
        List<ChandelierExitResult> resultList = new List<ChandelierExitResult>();
        List<PriceHistory> priceHistoryList = new List<PriceHistory>();

        int dir = 1;
        double normMax = double.NaN;
        double normMin = double.NaN;
        int prevOs = 0;

        List<double> normBuffer = new List<double>();

        foreach (PriceHistory priceHistory in priceHistories)
        {
            double close = priceHistory.Close;

            priceHistoryList.Add(priceHistory);
            Prices.Add(close);

            if (priceHistoryList.Count < AtrLength)
                continue;

            // -- ATR -----------------------------------------------------------
            ATR atr = new ATR(AtrLength);
            atr.PriceHistoryList = priceHistoryList;
            double atrValue = atr.Calculate().ResultData.Last<TimeSeriesData>().Value.Value;

            // -- Highest / Lowest over AtrLength bars --------------------------
            List<PriceHistory> window = priceHistoryList
                .Skip(priceHistoryList.Count - AtrLength)
                .ToList();

            double highestClose = window.Max(p => p.Close);
            double highestHigh = window.Max(p => p.High);
            double lowestClose = window.Min(p => p.Close);
            double lowestLow = window.Min(p => p.Low);

            double chandHigh = (highestClose + highestHigh) / 2.0;
            double chandLow = (lowestClose + lowestLow) / 2.0;

            double chandLong = chandHigh - atrValue * Multiplier;
            double chandShort = chandLow + atrValue * Multiplier;

            // -- Direction -----------------------------------------------------
            int prevDir = dir;
            if (close > chandShort) dir = 1;
            else if (close < chandLong) dir = -1;

            double exitLevel = dir > 0 ? chandLong : chandShort;

            // -- Normalized oscillator (0-100) ---------------------------------
            double midChand = (chandLong + chandShort) / 2.0;
            bool isBull = close > midChand;
            bool isBear = close < midChand;

            int os = isBull ? 1 : isBear ? -1 : prevOs;

            if (os > prevOs)
                normMax = close;
            else if (os < prevOs)
                normMin = close;
            else
            {
                normMax = double.IsNaN(normMax) ? close : Math.Max(close, normMax);
                normMin = double.IsNaN(normMin) ? close : Math.Min(close, normMin);
            }

            prevOs = os;

            double normRaw = 0.0;
            double range = normMax - normMin;
            if (range > 0)
                normRaw = (close - normMin) / range * 100.0;

            // -- SMA smoothing -------------------------------------------------
            normBuffer.Add(normRaw);
            if (normBuffer.Count > Smoothing)
                normBuffer.RemoveAt(0);

            double oscillator = normBuffer.Average();

            // -- Build result --------------------------------------------------
            resultList.Add(new ChandelierExitResult()
            {
                Date = priceHistory.Date,
                Direction = dir,
                ExitLevel = exitLevel,
                Oscillator = oscillator,
                DirectionChanged = dir != prevDir
            });
        }

        return resultList;
    }
}