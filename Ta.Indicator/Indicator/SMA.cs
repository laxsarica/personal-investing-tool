using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;

namespace TA.Indicators.Indicator;

public class SMA : IndicatorCalculatorBase<Result>
{
  public override List<PriceHistory> PriceHistoryList { get; set; }

  protected int Period { get; set; }

  public SMA(int period) => this.Period = period;

  public override Result Calculate()
  {
    Result result = new Result();
    for (int index1 = 0; index1 < this.PriceHistoryList.Count; ++index1)
    {
      if (index1 >= this.Period - 1)
      {
        double num1 = 0.0;
        for (int index2 = index1; index2 >= index1 - (this.Period - 1); --index2)
          num1 += this.PriceHistoryList[index2].Close;
        double num2 = num1 / (double) this.Period;
        result.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(num2),
          DateTime = this.PriceHistoryList[index1].Date
        });
      }
      else
        result.ResultData.Add((TimeSeriesData) null);
    }
    return result;
  }
}
