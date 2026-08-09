
using System.Runtime.Intrinsics.X86;
using Ta.Indicator.Base;
using TA.Indicators.Indicator;

namespace Ta.CustomIndicator.ZeroLag;

public class ZeroLagIndicator
{
    public int Length { get; set; } = 15;

    public bool ShowLevels { get; set; } = true;

    public int AtrLength { get; set; } = 16 /*0x10*/;

    public List<double> Prices { get; set; } = new List<double>();

    public List<double> ATRValues { get; set; } = new List<double>();

    public List<ZeroLagResult> Calculate(List<PriceHistory> priceHistories)
    {
        List<ZeroLagResult> zeroLagResultList = new List<ZeroLagResult>();
        List<PriceHistory> priceHistoryList = new List<PriceHistory>();
        foreach (PriceHistory priceHistory in priceHistories)
        {
            double close = priceHistory.Close;
            priceHistoryList.Add(priceHistory);
            if (priceHistoryList.Count < this.AtrLength)
            {
                this.Prices.Add(close);
            }
            else
            {
                ATR atr = new ATR(this.AtrLength);
                atr.PriceHistoryList = priceHistoryList;
                atr.Calculate().ResultData.Last<TimeSeriesData>();
                EMA ema = new EMA(this.Length);
                ema.PriceHistoryList = priceHistoryList;
                double num1 = ema.Calculate().ResultData.Last<TimeSeriesData>().Value.Value;
                this.Prices.Add(close + (close - num1));
                double num2 = this.CalculateEMA(this.Prices, this.Length).Last<double>();
                bool flag = num2 > num1;
                int num3 = num2 < num1 ? 1 : 0;
                if (flag)
                    zeroLagResultList.Add(new ZeroLagResult()
                    {
                        Date = priceHistory.Date,
                        UpSignal = true,
                        DownSignal = false
                    });
                if (num3 != 0)
                    zeroLagResultList.Add(new ZeroLagResult()
                    {
                        Date = priceHistory.Date,
                        UpSignal = false,
                        DownSignal = true
                    });
            }
        }
        return zeroLagResultList;
    }

    private List<double> CalculateEMA(List<double> prices, int period)
    {
        if (prices == null || prices.Count < period)
            throw new ArgumentException("Insufficient data to calculate EMA.");
        List<double> ema = new List<double>();
        double num1 = 2.0 / (double)(period + 1);
        double num2 = 0.0;
        for (int index = 0; index < period; ++index)
            num2 += prices[index];
        double num3 = num2 / (double)period;
        ema.Add(num3);
        for (int index = period; index < prices.Count; ++index)
        {
            double num4 = (prices[index] - ema[ema.Count - 1]) * num1 + ema[ema.Count - 1];
            ema.Add(num4);
        }
        return ema;
    }
}
