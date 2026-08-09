using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;

namespace TA.Indicators.Indicator;

public class WMA : IndicatorCalculatorBase<Result>
{
  public override List<PriceHistory> PriceHistoryList { get; set; }

  protected int Period { get; set; }

  public WMA(int period) => this.Period = period;

  public override Result Calculate()
  {
    Result result = new Result();
    int num1 = 0;
    for (int index = 1; index <= this.Period; ++index)
      num1 += index;
    for (int index1 = 0; index1 < this.PriceHistoryList.Count; ++index1)
    {
      if (index1 >= this.Period - 1)
      {
        double num2 = 0.0;
        int num3 = 1;
        for (int index2 = index1 - (this.Period - 1); index2 <= index1; ++index2)
        {
          num2 += (double) num3 / (double) num1 * this.PriceHistoryList[index2].Close;
          ++num3;
        }
        result.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(num2),
          DateTime = this.PriceHistoryList[index1].Date
        });
      }
      else
        result.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(0.0),
          DateTime = this.PriceHistoryList[index1].Date
        });
    }
    return result;
  }
}
