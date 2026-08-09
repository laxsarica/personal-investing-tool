using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;

namespace TA.Indicators.Indicator;

public class ATR : IndicatorCalculatorBase<Result>
{
  public override List<PriceHistory> PriceHistoryList { get; set; }

  protected int Period { get; set; }

  public ATR(int period) => this.Period = period;

  public override Result Calculate()
  {
    Result result = new Result();
    for (int index = 0; index < this.PriceHistoryList.Count; ++index)
    {
      List<double> source = new List<double>(3)
      {
        this.PriceHistoryList[index].High - this.PriceHistoryList[index].Low
      };
      if (index > 0)
      {
        source.Add(Math.Abs(this.PriceHistoryList[index].High - this.PriceHistoryList[index - 1].Close));
        source.Add(Math.Abs(this.PriceHistoryList[index].Low - this.PriceHistoryList[index - 1].Close));
      }
      double num1 = source.Max();
      if (index == this.Period - 1)
        result.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(source.Average()),
          DateTime = this.PriceHistoryList[index].Date
        });
      else if (index > this.Period - 1)
      {
        double? nullable1 = result.ResultData.Last<TimeSeriesData>().Value;
        double num2 = (double) (this.Period - 1);
        double? nullable2 = nullable1.HasValue ? new double?(nullable1.GetValueOrDefault() * num2) : new double?();
        double num3 = num1;
        double? nullable3;
        if (!nullable2.HasValue)
        {
          nullable1 = new double?();
          nullable3 = nullable1;
        }
        else
          nullable3 = new double?(nullable2.GetValueOrDefault() + num3);
        double? nullable4 = nullable3;
        double period = (double) this.Period;
        double? nullable5;
        if (!nullable4.HasValue)
        {
          nullable2 = new double?();
          nullable5 = nullable2;
        }
        else
          nullable5 = new double?(nullable4.GetValueOrDefault() / period);
        double? nullable6 = nullable5;
        result.ResultData.Add(new TimeSeriesData()
        {
          Value = nullable6,
          DateTime = this.PriceHistoryList[index].Date
        });
      }
      else
        result.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(0.0),
          DateTime = this.PriceHistoryList[index].Date
        });
    }
    return result;
  }
}
