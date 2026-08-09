using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;

namespace TA.Indicators.Indicator;

public class KAMA : IndicatorCalculatorBase<Result>
{
  private double fastEnd = 0.666;
  private double slowEnd = 0.0645;

  public override List<PriceHistory> PriceHistoryList { get; set; }

  protected int Period { get; set; }

  public KAMA(int period) => this.Period = period;

  public override Result Calculate()
  {
    Result result = new Result();
    int count = this.PriceHistoryList.Count;
    double[] source = new double[count];
    double[] numArray1 = new double[count];
    double[] numArray2 = new double[count];
    double[] numArray3 = new double[count];
    double[] numArray4 = new double[count];
    for (int index = 1; index < count; ++index)
      source[index] = Math.Abs(this.PriceHistoryList[index].Close - this.PriceHistoryList[index - 1].Close);
    for (int index = 0; index < count; ++index)
    {
      if (index >= this.Period)
      {
        double? nullable1 = result.ResultData[index - 1].Value;
        numArray1[index] = Math.Abs(this.PriceHistoryList[index].Close - this.PriceHistoryList[index - this.Period].Close);
        numArray2[index] = ((IEnumerable<double>) source).Skip<double>(index - this.Period + 1).Take<double>(this.Period).Sum();
        numArray3[index] = numArray2[index] != 0.0 ? numArray1[index] / numArray2[index] : 0.0;
        numArray4[index] = Math.Pow(numArray3[index] * (this.fastEnd - this.slowEnd) + this.slowEnd, 2.0);
        double? nullable2 = nullable1;
        double num = numArray4[index];
        double close = this.PriceHistoryList[index].Close;
        double? nullable3 = nullable1;
        double? nullable4 = nullable3.HasValue ? new double?(close - nullable3.GetValueOrDefault()) : new double?();
        double? nullable5;
        if (!nullable4.HasValue)
        {
          nullable3 = new double?();
          nullable5 = nullable3;
        }
        else
          nullable5 = new double?(num * nullable4.GetValueOrDefault());
        double? nullable6 = nullable5;
        double? nullable7;
        if (!(nullable2.HasValue & nullable6.HasValue))
        {
          nullable4 = new double?();
          nullable7 = nullable4;
        }
        else
          nullable7 = new double?(nullable2.GetValueOrDefault() + nullable6.GetValueOrDefault());
        double? nullable8 = nullable7;
        result.ResultData.Add(new TimeSeriesData()
        {
          Value = nullable8,
          DateTime = this.PriceHistoryList[index].Date
        });
      }
      else
      {
        double close = this.PriceHistoryList.Skip<PriceHistory>(index).Take<PriceHistory>(1).First<PriceHistory>().Close;
        result.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(close),
          DateTime = this.PriceHistoryList[index].Date
        });
      }
    }
    return result;
  }
}
