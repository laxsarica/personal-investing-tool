using System;
using System.Collections.Generic;
using System.Linq;

namespace TradingIndicators
{
    public class SRHighVolumeCore
    {
        public class Result
        {
            public double[] Support;        // support pivot value
            public double[] SupportLower;   // supportLevel_1 = support - width
            public double[] Resistance;     // resistance pivot value
            public double[] ResistanceUpper;// resistanceLevel_1 = resistance + width

            public bool[] BreakoutSup;
            public bool[] BreakoutRes;
            public bool[] SupHolds;
            public bool[] ResHolds;
        }

        public static Result Process(
            double[] open,
            double[] high,
            double[] low,
            double[] close,
            double[] volume,
            int lookbackPeriod = 20,
            int volLen = 2,
            double boxWidthFactor = 1.0)
        {
            int n = close.Length;
            var support = Enumerable.Repeat(double.NaN, n).ToArray();
            var supportLower = Enumerable.Repeat(double.NaN, n).ToArray();
            var resistance = Enumerable.Repeat(double.NaN, n).ToArray();
            var resistanceUpper = Enumerable.Repeat(double.NaN, n).ToArray();

            var breakoutSup = new bool[n];
            var breakoutRes = new bool[n];
            var supHolds = new bool[n];
            var resHolds = new bool[n];

            // Signed volume
            double[] signedVol = new double[n];
            for (int i = 0; i < n; i++)
                signedVol[i] = (close[i] > open[i]) ? volume[i] : -volume[i];

            // Pivot detection
            double?[] pivotHighAt = new double?[n];
            double?[] pivotLowAt = new double?[n];

            for (int c = lookbackPeriod; c <= n - lookbackPeriod - 1; c++)
            {
                int start = c - lookbackPeriod;
                int end = c + lookbackPeriod;
                double v = close[c];

                bool isHigh = true;
                bool isLow = true;

                for (int k = start; k <= end; k++)
                {
                    if (k == c) continue;
                    if (close[k] >= v) isHigh = false;
                    if (close[k] <= v) isLow = false;
                    if (!isHigh && !isLow) break;
                }

                int det = c + lookbackPeriod;
                if (det < n)
                {
                    if (isHigh) pivotHighAt[det] = v;
                    if (isLow) pivotLowAt[det] = v;
                }
            }

            // ATR(200)
            int ATR_LENGTH = 200;
            double[] atr = new double[n];
            double[] tr = new double[n];

            for (int i = 0; i < n; i++)
            {
                if (i == 0)
                    tr[i] = high[i] - low[i];
                else
                    tr[i] = Math.Max(high[i] - low[i], Math.Max(Math.Abs(high[i] - close[i - 1]), Math.Abs(low[i] - close[i - 1])));
            }

            Queue<double> q = new Queue<double>();
            double sum = 0;

            for (int i = 0; i < n; i++)
            {
                sum += tr[i];
                q.Enqueue(tr[i]);
                if (q.Count > ATR_LENGTH)
                    sum -= q.Dequeue();

                atr[i] = (q.Count == ATR_LENGTH) ? (sum / ATR_LENGTH) : double.NaN;
            }

            // Helper highest/lowest over last X bars
            double Highest(double[] arr, int idx, int len)
            {
                int start = Math.Max(0, idx - len + 1);
                double max = double.NegativeInfinity;
                for (int i = start; i <= idx; i++)
                    if (!double.IsNaN(arr[i]) && arr[i] > max) max = arr[i];
                return double.IsNegativeInfinity(max) ? double.NaN : max;
            }

            double Lowest(double[] arr, int idx, int len)
            {
                int start = Math.Max(0, idx - len + 1);
                double min = double.PositiveInfinity;
                for (int i = start; i <= idx; i++)
                    if (!double.IsNaN(arr[i]) && arr[i] < min) min = arr[i];
                return double.IsPositiveInfinity(min) ? double.NaN : min;
            }

            // Internal tracking
            double currSup = double.NaN, currSup1 = double.NaN;
            double currRes = double.NaN, currRes1 = double.NaN;

            double prevLow = double.NaN, prevHigh = double.NaN;
            double prevSup = double.NaN, prevSup1 = double.NaN;
            double prevRes = double.NaN, prevRes1 = double.NaN;

            // MAIN LOOP
            for (int i = 0; i < n; i++)
            {
                double Vol = signedVol[i];

                // volume thresholds
                double vol_hi = Highest(signedVol.Select(v => v / 2.5).ToArray(), i, volLen);
                double vol_lo = Lowest(signedVol.Select(v => v / 2.5).ToArray(), i, volLen);

                double wd = atr[i] * boxWidthFactor;

                // SUPPORT detection
                if (pivotLowAt[i].HasValue && Vol > vol_hi && !double.IsNaN(wd))
                {
                    currSup = pivotLowAt[i].Value;
                    currSup1 = currSup - wd;
                }

                // RESISTANCE detection
                if (pivotHighAt[i].HasValue && Vol < vol_lo && !double.IsNaN(wd))
                {
                    currRes = pivotHighAt[i].Value;
                    currRes1 = currRes + wd;
                }

                support[i] = currSup;
                supportLower[i] = currSup1;
                resistance[i] = currRes;
                resistanceUpper[i] = currRes1;

                if (i == 0)
                {
                    prevLow = low[i];
                    prevHigh = high[i];
                    prevSup = currSup;
                    prevSup1 = currSup1;
                    prevRes = currRes;
                    prevRes1 = currRes1;
                    continue;
                }

                // BREAKOUT & HOLDS
                breakoutRes[i] = (!double.IsNaN(prevRes1) && !double.IsNaN(currRes1))
                                 && (prevLow <= prevRes1 && low[i] > currRes1);

                resHolds[i] = (!double.IsNaN(prevRes) && !double.IsNaN(currRes))
                               && (prevHigh >= prevRes && high[i] < currRes);

                supHolds[i] = (!double.IsNaN(prevSup) && !double.IsNaN(currSup))
                               && (prevLow <= prevSup && low[i] > currSup);

                breakoutSup[i] = (!double.IsNaN(prevSup1) && !double.IsNaN(currSup1))
                                  && (prevHigh >= prevSup1 && high[i] < currSup1);

                // update prev
                prevLow = low[i];
                prevHigh = high[i];
                prevSup = currSup;
                prevSup1 = currSup1;
                prevRes = currRes;
                prevRes1 = currRes1;
            }

            return new Result
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
    }
}
