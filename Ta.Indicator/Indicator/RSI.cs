using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;

namespace TA.Indicators.Indicator;

public class RSI : IndicatorCalculatorBase<Result>
{
  public override List<PriceHistory> PriceHistoryList { get; set; }

  protected int Period { get; set; }

  public RSI(int period) => this.Period = period;

  public override Result Calculate()
  {
    Result result = new Result();
    double num1 = 0.0;
    double num2 = 0.0;
    double num3 = 0.0;
    double num4 = 0.0;
    for (int index = 1; index < this.PriceHistoryList.Count; ++index)
    {
      double num5 = this.PriceHistoryList[index].Close - this.PriceHistoryList[index - 1].Close;
      if (index < this.Period)
      {
        if (num5 > 0.0)
          num1 += num5;
        else
          num2 += -1.0 * num5;
        num3 = num1 / (double) this.Period;
        num4 = num2 / (double) this.Period;
        double num6 = 100.0 - 100.0 / (1.0 + num3 / num4);
        result.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(num6),
          DateTime = this.PriceHistoryList[index].Date
        });
      }
      else
      {
        if (num5 > 0.0)
        {
          num3 = (num3 * (double) (this.Period - 1) + num5) / (double) this.Period;
          num4 = num4 * (double) (this.Period - 1) / (double) this.Period;
        }
        else
        {
          num3 = num3 * (double) (this.Period - 1) / (double) this.Period;
          num4 = (num4 * (double) (this.Period - 1) + -1.0 * num5) / (double) this.Period;
        }
        double num7 = 100.0 - 100.0 / (1.0 + num3 / num4);
        result.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(num7),
          DateTime = this.PriceHistoryList[index].Date
        });
      }
    }
    return result;
  }
}
