using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Ta.CustomIndicator.EmaFifty;
using Ta.Indicator.Base;

namespace Ta.CustomIndicator.BreakOut;

public class SupportResistanceBreakOutIndicator
{

    public List<SRResult> Calculate(List<PriceHistory> priceHistories)
    {
        List<SRResult> sRResults = new List<SRResult>();
        var (open, high, low, close, volume) = ConvertCandles(priceHistories);
        var result = Process(open, high, low, close, volume, lookback: 20, volLen: 2, boxWidthFactor: 1.0);
        for (int i = 0; i < priceHistories.Count; i++)
        {
            if (result.BreakoutSup[i])
            {
                sRResults.Add(new SRResult
                {
                    Date = priceHistories[i].Date,
                    DownSignal = true,
                    UpSignal = false
                });
            }
            if (result.BreakoutRes[i])
            {
                sRResults.Add(new SRResult
                {
                    Date = priceHistories[i].Date,
                    DownSignal = false,
                    UpSignal = true
                });
            }
            if (result.SupHolds[i])
            {
                sRResults.Add(new SRResult
                {
                    Date = priceHistories[i].Date,
                    DownSignal = false,
                    UpSignal = true
                });
            }
            if (result.ResHolds[i])
            {
                sRResults.Add(new SRResult
                {
                    Date = priceHistories[i].Date,
                    DownSignal = true,
                    UpSignal = false
                });
            }
        }
        return sRResults;
    }
    private InternalResult Process(double[] open, double[] high, double[] low, double[] close, double[] volume, int lookback = 20, int volLen = 2, double boxWidthFactor = 1.0)
    {
        int n = close.Length;

        var signedVol = ComputeSignedVolume(open, close, volume);
        var (pivotHigh, pivotLow) = DetectPivots(close, lookback);
        var atr = ComputeATR(high, low, close);

        var support = new double[n];
        var supportLower = new double[n];
        var resistance = new double[n];
        var resistanceUpper = new double[n];

        var breakoutSup = new bool[n];
        var breakoutRes = new bool[n];
        var supHolds = new bool[n];
        var resHolds = new bool[n];

        double currSup = double.NaN, currSup1 = double.NaN;
        double currRes = double.NaN, currRes1 = double.NaN;

        double prevLow = double.NaN, prevHigh = double.NaN;
        double prevSup = double.NaN, prevSup1 = double.NaN;
        double prevRes = double.NaN, prevRes1 = double.NaN;

        for (int i = 0; i < n; i++)
        {
            double volHi = Highest(signedVol, i, volLen) / 2.5;
            double volLo = Lowest(signedVol, i, volLen) / 2.5;
            double width = atr[i] * boxWidthFactor;

            UpdateSupport(ref currSup, ref currSup1, pivotLow[i], signedVol[i], volHi, width);
            UpdateResistance(ref currRes, ref currRes1, pivotHigh[i], signedVol[i], volLo, width);

            support[i] = currSup;
            supportLower[i] = currSup1;
            resistance[i] = currRes;
            resistanceUpper[i] = currRes1;

            if (i > 0)
            {
                DetectSignals(i,
                    low, high,
                    prevLow, prevHigh,
                    prevSup, prevSup1,
                    prevRes, prevRes1,
                    currSup, currSup1,
                    currRes, currRes1,
                    breakoutSup, breakoutRes, supHolds, resHolds);
            }

            prevLow = low[i];
            prevHigh = high[i];
            prevSup = currSup;
            prevSup1 = currSup1;
            prevRes = currRes;
            prevRes1 = currRes1;
        }

        return new InternalResult
        {
            Support = support,
            SupportLower = supportLower,
            Resistance = resistance,
            ResistanceUpper = resistanceUpper,
            BreakoutSup = breakoutSup,
            BreakoutRes = breakoutRes,
            SupHolds = supHolds,
            ResHolds = resHolds
        };
    }

    // ------------------------------------------------------------
    // PRIVATE HELPERS
    // ------------------------------------------------------------

    private double[] ComputeSignedVolume(double[] open, double[] close, double[] vol)
    {
        int n = vol.Length;
        double[] outVol = new double[n];

        for (int i = 0; i < n; i++)
            outVol[i] = (close[i] > open[i]) ? vol[i] : -vol[i];

        return outVol;
    }

    private (double?[] pivotHigh, double?[] pivotLow) DetectPivots(double[] close, int len)
    {
        int n = close.Length;

        double?[] ph = new double?[n];
        double?[] pl = new double?[n];

        for (int c = len; c <= n - len - 1; c++)
        {
            int start = c - len;
            int end = c + len;
            double v = close[c];

            bool isHigh = true, isLow = true;

            for (int k = start; k <= end; k++)
            {
                if (k == c) continue;
                if (close[k] >= v) isHigh = false;
                if (close[k] <= v) isLow = false;
                if (!isHigh && !isLow) break;
            }

            int det = c + len;
            if (det < n)
            {
                if (isHigh) ph[det] = v;
                if (isLow) pl[det] = v;
            }
        }

        return (ph, pl);
    }

    private double[] ComputeATR(double[] high, double[] low, double[] close)
    {
        int n = high.Length;
        const int ATR_LEN = 200;

        double[] tr = new double[n];
        double[] atr = new double[n];
        Queue<double> q = new Queue<double>();
        double sum = 0;

        for (int i = 0; i < n; i++)
        {
            tr[i] = (i == 0)
                    ? high[i] - low[i]
                    : Math.Max(high[i] - low[i],
                       Math.Max(Math.Abs(high[i] - close[i - 1]), Math.Abs(low[i] - close[i - 1])));

            sum += tr[i];
            q.Enqueue(tr[i]);

            if (q.Count > ATR_LEN)
                sum -= q.Dequeue();

            atr[i] = (q.Count == ATR_LEN) ? sum / ATR_LEN : double.NaN;
        }

        return atr;
    }

    private double Highest(double[] arr, int idx, int len)
    {
        int start = Math.Max(0, idx - len + 1);
        double max = double.NegativeInfinity;

        for (int i = start; i <= idx; i++)
            if (arr[i] > max) max = arr[i];

        return max;
    }

    private double Lowest(double[] arr, int idx, int len)
    {
        int start = Math.Max(0, idx - len + 1);
        double min = double.PositiveInfinity;

        for (int i = start; i <= idx; i++)
            if (arr[i] < min) min = arr[i];

        return min;
    }

    private void UpdateSupport(ref double sup, ref double sup1, double? pivotLow, double volume, double volHi, double width)
    {
        if (pivotLow.HasValue && volume > volHi && !double.IsNaN(width))
        {
            sup = pivotLow.Value;
            sup1 = sup - width;
        }
    }

    private void UpdateResistance(ref double res, ref double res1, double? pivotHigh, double volume, double volLo, double width)
    {
        if (pivotHigh.HasValue && volume < volLo && !double.IsNaN(width))
        {
            res = pivotHigh.Value;
            res1 = res + width;
        }
    }

    private void DetectSignals(
        int i,
        double[] low, double[] high,
        double prevLow, double prevHigh,
        double prevSup, double prevSup1,
        double prevRes, double prevRes1,
        double currSup, double currSup1,
        double currRes, double currRes1,
        bool[] breakoutSup, bool[] breakoutRes,
        bool[] supHolds, bool[] resHolds)
    {
        breakoutRes[i] =
            (!double.IsNaN(prevRes1) && !double.IsNaN(currRes1)) &&
            (prevLow <= prevRes1 && low[i] > currRes1);

        breakoutSup[i] =
            (!double.IsNaN(prevSup1) && !double.IsNaN(currSup1)) &&
            (prevHigh >= prevSup1 && high[i] < currSup1);

        resHolds[i] =
            (!double.IsNaN(prevRes) && !double.IsNaN(currRes)) &&
            (prevHigh >= prevRes && high[i] < currRes);

        supHolds[i] =
            (!double.IsNaN(prevSup) && !double.IsNaN(currSup)) &&
            (prevLow <= prevSup && low[i] > currSup);
    }
    private (double[] Open, double[] High, double[] Low, double[] Close, double[] Volume) ConvertCandles(List<PriceHistory> candles)
    {
        int n = candles.Count;

        double[] open = new double[n];
        double[] high = new double[n];
        double[] low = new double[n];
        double[] close = new double[n];
        double[] volume = new double[n];

        for (int i = 0; i < n; i++)
        {
            open[i] = candles[i].Open;
            high[i] = candles[i].High;
            low[i] = candles[i].Low;
            close[i] = candles[i].Close;
            volume[i] = candles[i].Volume;
        }

        return (open, high, low, close, volume);
    }
}

