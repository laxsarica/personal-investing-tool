using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;
using TA.Indicators.Indicator;

namespace Ta.CustomIndicator.RsiWma;

public class RsiWeightedMovingAverageIndicator
{
    public int RsiPeriod { get; set; } = 14;
    public int MaPeriod { get; set; } = 55;
    public bool ShowSma { get; set; } = true;

    public List<RsiWmaResult> Calculate(List<PriceHistory> priceHistories)
    {
        List<RsiWmaResult> results = new List<RsiWmaResult>();
        List<PriceHistory> priceHistoryList = new List<PriceHistory>();
        List<double> closePrices = new List<double>();

        // ── BUG FIX 1 ────────────────────────────────────────────────────────
        // rsi_series[i] corresponds to closes[i+1], so the oldest bar in an
        // MA_PERIOD-wide WMA window needs ri = idx - (MA_PERIOD-1) - 1 >= 0
        // → idx >= MA_PERIOD.
        // Previous code used Max(RsiPeriod+1, MaPeriod) which allowed idx = MaPeriod-1
        // at exactly MA_PERIOD=55 bars, causing ri = -1 and rsi_wma returning null.
        // Correct minimum: idx must be >= MaPeriod, i.e. we need MaPeriod+1 bars.
        // ─────────────────────────────────────────────────────────────────────
        int minBars = Math.Max(RsiPeriod + 1, MaPeriod + 1);

        foreach (PriceHistory priceHistory in priceHistories)
        {
            double close = priceHistory.Close;
            priceHistoryList.Add(priceHistory);
            closePrices.Add(close);

            if (priceHistoryList.Count < minBars)
                continue;

            // ── RSI via existing RSI class ────────────────────────────────────
            RSI rsi = new RSI(RsiPeriod);
            rsi.PriceHistoryList = priceHistoryList;
            List<TimeSeriesData> rsiData = rsi.Calculate().ResultData;
            // rsiData.Count == priceHistoryList.Count - 1
            // rsiData[i] corresponds to closePrices[i+1]

            if (rsiData.Count < MaPeriod)
                continue;

            // ── RSI-Weighted MA ───────────────────────────────────────────────
            double rsiWma = CalculateRsiWeightedMA(closePrices, rsiData, MaPeriod);

            // ── SMA via existing SMA class ────────────────────────────────────
            SMA sma = new SMA(MaPeriod);
            sma.PriceHistoryList = priceHistoryList;
            List<TimeSeriesData> smaData = sma.Calculate().ResultData;
            double smaValue = smaData.Last()?.Value ?? double.NaN;

            double currentRsi = rsiData.Last().Value ?? 0.0;
            double deviation = !double.IsNaN(smaValue) && smaValue != 0
                ? (rsiWma - smaValue) / smaValue * 100.0
                : 0.0;

            // ── BUG FIX 2 ────────────────────────────────────────────────────
            // Pine Script ta.crossover / ta.crossunder use STRICT inequalities:
            //   crossover(src, sig)  → src[1] < sig[1]  AND src > sig   (strictly less then above)
            //   crossunder(src, sig) → src[1] > sig[1]  AND src < sig   (strictly above then less)
            //
            // Previous code used <= and >= which could generate false signals
            // when the price sits exactly on the RSI-WMA for one bar.
            // ─────────────────────────────────────────────────────────────────
            bool crossAbove = false;
            bool crossBelow = false;

            if (results.Count > 0)
            {
                RsiWmaResult prev = results.Last();
                double prevClose = closePrices[closePrices.Count - 2];

                crossAbove = prevClose < prev.RsiWma && close > rsiWma;   // strict <  and >
                crossBelow = prevClose > prev.RsiWma && close < rsiWma;   // strict >  and <
            }

            results.Add(new RsiWmaResult
            {
                Date = priceHistory.Date,
                RsiValue = currentRsi,
                RsiWma = rsiWma,
                Sma = ShowSma ? smaValue : double.NaN,
                DeviationFromSma = deviation,
                BullishCross = crossAbove,
                BearishCross = crossBelow
            });
        }

        return results;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RSI-Weighted Moving Average
    //
    // Pine Script equivalent:
    //   for i = 0 to length-1
    //       weight = rsiVal[i] / 100
    //       sumWeightedPrice += src[i] * weight
    //       sumWeights       += weight
    //   rsiWMA = sumWeightedPrice / sumWeights
    //
    // Alignment note:
    //   closePrices[n]   = bar n close
    //   rsiData[n]       = RSI for bar n+1   (your RSI class starts from index 1)
    //
    //   Walking back from the most recent bar:
    //     price  = closePrices[closePrices.Count - 1 - i]
    //     rsi    = rsiData   [rsiData.Count    - 1 - i]   ← same offset because
    //              rsiData.Count = closePrices.Count - 1,
    //              so rsiData[rsiData.Count-1-i] = RSI of closePrices[closePrices.Count-1-i]
    // ─────────────────────────────────────────────────────────────────────────
    private double CalculateRsiWeightedMA(
        List<double> closePrices,
        List<TimeSeriesData> rsiData,
        int length)
    {
        double sumWeightedPrice = 0.0;
        double sumWeights = 0.0;

        for (int i = 0; i < length; i++)
        {
            double price = closePrices[closePrices.Count - 1 - i];
            double rsi = rsiData[rsiData.Count - 1 - i].Value ?? 50.0;
            double weight = rsi / 100.0;

            sumWeightedPrice += price * weight;
            sumWeights += weight;
        }

        return sumWeights > 0 ? sumWeightedPrice / sumWeights : closePrices.Last();
    }
}