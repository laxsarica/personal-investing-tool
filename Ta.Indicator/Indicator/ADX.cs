using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;


namespace TA.Indicators.Indicator;

public class ADX : IndicatorCalculatorBase<Result>
{
    public override List<PriceHistory> PriceHistoryList { get; set; }
    protected int Period { get; set; }

    public ADX(int period) => this.Period = period;

    public override Result Calculate()
    {
        Result result = new Result();

        List<double> plusDM = new List<double>();
        List<double> minusDM = new List<double>();
        List<double> dxList = new List<double>();

        // Step 1: Calculate +DM and -DM
        for (int i = 0; i < PriceHistoryList.Count; i++)
        {
            if (i == 0)
            {
                plusDM.Add(0);
                minusDM.Add(0);
                continue;
            }

            double upMove = PriceHistoryList[i].High - PriceHistoryList[i - 1].High;
            double downMove = PriceHistoryList[i - 1].Low - PriceHistoryList[i].Low;

            plusDM.Add((upMove > downMove && upMove > 0) ? upMove : 0);
            minusDM.Add((downMove > upMove && downMove > 0) ? downMove : 0);
        }

        // Step 2: Smooth +DM and -DM (Wilder smoothing)
        double smoothedPlusDM = plusDM.Take(Period).Sum();
        double smoothedMinusDM = minusDM.Take(Period).Sum();

        // ATR (reuse your existing ATR calculator)
        ATR atrCalculator = new ATR(Period)
        {
            PriceHistoryList = this.PriceHistoryList
        };
        Result atrResult = atrCalculator.Calculate();

        for (int i = 0; i < PriceHistoryList.Count; i++)
        {
            if (i < Period)
            {
                result.ResultData.Add(new TimeSeriesData
                {
                    DateTime = PriceHistoryList[i].Date,
                    Value = 0
                });
                continue;
            }

            if (i > Period)
            {
                smoothedPlusDM = smoothedPlusDM - (smoothedPlusDM / Period) + plusDM[i];
                smoothedMinusDM = smoothedMinusDM - (smoothedMinusDM / Period) + minusDM[i];
            }

            double atr = atrResult.ResultData[i].Value ?? 0;
            if (atr == 0)
            {
                result.ResultData.Add(new TimeSeriesData
                {
                    DateTime = PriceHistoryList[i].Date,
                    Value = 0
                });
                continue;
            }

            double plusDI = 100 * (smoothedPlusDM / atr);
            double minusDI = 100 * (smoothedMinusDM / atr);

            double dx = 100 * Math.Abs(plusDI - minusDI) / (plusDI + minusDI);
            dxList.Add(dx);

            // Step 3: ADX calculation
            if (dxList.Count == Period)
            {
                double firstADX = dxList.Average();
                result.ResultData.Add(new TimeSeriesData
                {
                    DateTime = PriceHistoryList[i].Date,
                    Value = firstADX
                });
            }
            else if (dxList.Count > Period)
            {
                double prevADX = result.ResultData.Last().Value ?? 0;
                double adx = ((prevADX * (Period - 1)) + dx) / Period;

                result.ResultData.Add(new TimeSeriesData
                {
                    DateTime = PriceHistoryList[i].Date,
                    Value = adx
                });
            }
            else
            {
                result.ResultData.Add(new TimeSeriesData
                {
                    DateTime = PriceHistoryList[i].Date,
                    Value = 0
                });
            }
        }

        return result;
    }
}

