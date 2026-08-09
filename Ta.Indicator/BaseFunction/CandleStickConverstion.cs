using Ta.Indicator.Base;

namespace Ta.Indicator.BaseFunction;

public class CandleStickConverstion
{
    public static List<PriceHistory> ToHeikinAshi(List<PriceHistory> candles)
    {
        List<PriceHistory> heikinAshi = new List<PriceHistory>();
        foreach (PriceHistory candle in candles)
        {
            PriceHistory priceHistory = new PriceHistory();
            priceHistory.Close = (candle.Open + candle.High + candle.Low + candle.Close) / 4.0;
            priceHistory.Date = candle.Date;
            if (heikinAshi.Count == 0)
            {
                priceHistory.Open = candle.Open;
                priceHistory.High = candle.High;
                priceHistory.Low = candle.Low;
            }
            else
            {
                priceHistory.Open = (heikinAshi[heikinAshi.Count - 1].Open + heikinAshi[heikinAshi.Count - 1].Close) / 2.0;
                priceHistory.High = Math.Max(candle.High, Math.Max(priceHistory.Open, priceHistory.Close));
                priceHistory.Low = Math.Min(candle.Low, Math.Min(priceHistory.Open, priceHistory.Close));
            }
            heikinAshi.Add(priceHistory);
        }
        return heikinAshi;
    }
}
