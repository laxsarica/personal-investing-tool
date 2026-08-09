using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;

namespace TA.Indicators.Indicator;

public class EMA : IndicatorCalculatorBase<Result>
{
  protected bool Wilder;

  public override List<PriceHistory> PriceHistoryList { get; set; }

  protected int Period { get; set; }

  protected ColumnType ColumnType { get; set; } = ColumnType.Close;

  public EMA(int period) => this.Period = period;

  public override Result Calculate()
  {
    Result result = new Result();
    double num1 = 2.0 / (double) (this.Period + 1);
    for (int index = 0; index < this.PriceHistoryList.Count; ++index)
    {
      if (index >= this.Period)
      {
        double? nullable = result.ResultData[index - 1].Value;
        double num2 = (this.PriceHistoryList[index].Close - nullable.Value) * num1 + nullable.Value;
        result.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(num2),
          DateTime = this.PriceHistoryList[index].Date
        });
      }
      else
      {
        double num3 = this.PriceHistoryList.Take<PriceHistory>(index + 1).Select<PriceHistory, double>((Func<PriceHistory, double>) (s => s.Close)).Average();
        result.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(num3),
          DateTime = this.PriceHistoryList[index].Date
        });
      }
    }
    return result;
  }
}
