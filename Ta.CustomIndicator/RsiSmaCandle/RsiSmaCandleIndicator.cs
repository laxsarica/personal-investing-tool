using Ta.Indicator.Base;
using TA.Indicators.Indicator;

namespace Ta.CustomIndicator.RsiSmaCandle;

/// <summary>
/// RSI-SMA Candle (Logic 2) Indicator.
///
/// Pine Script translation of "Logic 2 RSI-SMA Candle [VivekSingh]".
///
/// Pipeline:
///   1. RSI(RsiPeriod) on close
///   2. SMA(MaPeriod) of each OHLC column
///   3. RSI-Weighted MA of each SMA OHLC:
///      f_rsiWMA(src, rsi, length) = SMA(src × rsi, length) / SMA(rsi, length)
///   4. BUY signal: close crosses above Logic2High AND RSI > RsiBuyThreshold
/// </summary>
public class RsiSmaCandleIndicator
{
    /// <summary>SMA period for OHLC smoothing and RSI-weighted MA window.</summary>
    public int MaPeriod { get; set; } = 30;

    /// <summary>RSI period.</summary>
    public int RsiPeriod { get; set; } = 14;

    /// <summary>RSI must be above this for a BUY signal.</summary>
    public double RsiBuyThreshold { get; set; } = 60.0;

    public List<RsiSmaCandleResult> Calculate(List<PriceHistory> priceHistories)
    {
        var results = new List<RsiSmaCandleResult>();

        if (priceHistories == null || priceHistories.Count < MinBarsRequired())
            return results;

        // ── Step 0: Compute RSI series ──────────────────────────────────────
        // RSI class returns Count-1 elements (starts from index 1).
        // rsiSeries[i] corresponds to priceHistories[i+1].
        var rsi = new RSI(RsiPeriod);
        rsi.PriceHistoryList = priceHistories;
        var rsiSeries = rsi.Calculate().ResultData;

        // ── Step 1: Build raw OHLC + RSI arrays aligned by index ────────────
        // We align so that index j in our arrays corresponds to priceHistories[j].
        // rsiSeries has Count-1 elements starting from bar 1, so rsiValues[0] = NaN.
        int n = priceHistories.Count;
        double[] opens = new double[n];
        double[] highs = new double[n];
        double[] lows = new double[n];
        double[] closes = new double[n];
        double[] rsiValues = new double[n];

        for (int i = 0; i < n; i++)
        {
            opens[i] = priceHistories[i].Open;
            highs[i] = priceHistories[i].High;
            lows[i] = priceHistories[i].Low;
            closes[i] = priceHistories[i].Close;

            // rsiSeries[i] maps to priceHistories[i+1], so priceHistories[i] maps to rsiSeries[i-1]
            if (i >= 1 && (i - 1) < rsiSeries.Count)
                rsiValues[i] = rsiSeries[i - 1].Value ?? 50.0;
            else
                rsiValues[i] = 50.0; // default for warmup
        }

        // ── Step 2: SMA(MaPeriod) of each OHLC ─────────────────────────────
        double[] smaOpen = SmaOnArray(opens, MaPeriod);
        double[] smaHigh = SmaOnArray(highs, MaPeriod);
        double[] smaLow = SmaOnArray(lows, MaPeriod);
        double[] smaClose = SmaOnArray(closes, MaPeriod);

        // ── Step 3: RSI-Weighted MA of SMA OHLC ────────────────────────────
        // f_rsiWMA(src, rsi, length) = SMA(src * rsi, length) / SMA(rsi, length)
        // Where rsi = rsiValue (broadcast to same length as src)
        double[] logic2Open = RsiWeightedSma(smaOpen, rsiValues, MaPeriod);
        double[] logic2High = RsiWeightedSma(smaHigh, rsiValues, MaPeriod);
        double[] logic2Low = RsiWeightedSma(smaLow, rsiValues, MaPeriod);
        double[] logic2Close = RsiWeightedSma(smaClose, rsiValues, MaPeriod);

        // ── Step 4: Build results with signal detection ─────────────────────
        double prevLogic2Close = double.NaN;
        double prevLogic2High = double.NaN;
        double prevClose = double.NaN;

        for (int i = 0; i < n; i++)
        {
            // Need valid Logic2 values (requires 2*MaPeriod - 1 warmup bars)
            if (double.IsNaN(logic2Close[i]))
            {
                prevClose = closes[i];
                continue;
            }

            double currentRsi = rsiValues[i];

            bool logic2Bull = logic2Close[i] >= logic2Open[i];
            bool logic2Rising = !double.IsNaN(prevLogic2Close) && (logic2Close[i] - prevLogic2Close) > 0;

            // BUY: ta.crossover(close, logic2High) AND rsiValue > RsiBuyThreshold
            // Crossover: previous close < previous logic2High AND current close > current logic2High
            bool buySignal = false;
            if (!double.IsNaN(prevClose) && !double.IsNaN(prevLogic2High))
            {
                buySignal = prevClose < prevLogic2High
                         && closes[i] > logic2High[i]
                         && currentRsi > RsiBuyThreshold;
            }

            results.Add(new RsiSmaCandleResult
            {
                Date = priceHistories[i].Date,
                Logic2Open = logic2Open[i],
                Logic2High = logic2High[i],
                Logic2Low = logic2Low[i],
                Logic2Close = logic2Close[i],
                Logic2Bull = logic2Bull,
                Logic2Rising = logic2Rising,
                RsiValue = currentRsi,
                BuySignal = buySignal
            });

            prevLogic2Close = logic2Close[i];
            prevLogic2High = logic2High[i];
            prevClose = closes[i];
        }

        return results;
    }

    /// <summary>Minimum bars required for valid output.</summary>
    public int MinBarsRequired() => MaPeriod * 2 + RsiPeriod;

    // ─────────────────────────────────────────────────────────────────────────
    // SMA on an arbitrary double array.
    // Returns NaN for indices where the window isn't full yet.
    //
    // Pine Script equivalent: ta.sma(series, length)
    // ─────────────────────────────────────────────────────────────────────────
    private static double[] SmaOnArray(double[] values, int period)
    {
        int n = values.Length;
        double[] result = new double[n];

        for (int i = 0; i < n; i++)
        {
            if (i < period - 1)
            {
                result[i] = double.NaN;
                continue;
            }

            // Explicit window sum — handles NaN inputs from previous SMA passes
            double sum = 0.0;
            bool hasNaN = false;
            for (int j = i - period + 1; j <= i; j++)
            {
                if (double.IsNaN(values[j]))
                {
                    hasNaN = true;
                    break;
                }
                sum += values[j];
            }

            result[i] = hasNaN ? double.NaN : sum / period;
        }

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RSI-Weighted SMA.
    //
    // Pine Script equivalent:
    //   f_rsiWMA(src, rsi, length) =>
    //       numerator   = ta.sma(src * rsi, length)
    //       denominator = ta.sma(rsi, length)
    //       denominator != 0 ? numerator / denominator : na
    //
    // Both src and rsi must be the same length as priceHistories.
    // Returns NaN where either SMA isn't ready or denominator is zero.
    // ─────────────────────────────────────────────────────────────────────────
    private static double[] RsiWeightedSma(double[] src, double[] rsi, int period)
    {
        int n = src.Length;

        // src * rsi
        double[] srcTimesRsi = new double[n];
        for (int i = 0; i < n; i++)
            srcTimesRsi[i] = double.IsNaN(src[i]) ? double.NaN : src[i] * rsi[i];

        // SMA(src * rsi, period) — numerator
        double[] numerator = SmaOnArray(srcTimesRsi, period);

        // SMA(rsi, period) — denominator
        double[] denominator = SmaOnArray(rsi, period);

        double[] result = new double[n];
        for (int i = 0; i < n; i++)
        {
            if (double.IsNaN(numerator[i]) || double.IsNaN(denominator[i]) || denominator[i] == 0)
                result[i] = double.NaN;
            else
                result[i] = numerator[i] / denominator[i];
        }

        return result;
    }
}
