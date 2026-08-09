using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;

namespace TA.Indicators.Indicator;

public class AMA : IndicatorCalculatorBase<Result>
{
  private const double FastestSC = 0.66666666666666663;
  private const double SlowestSC = 0.064516129032258063;

  public override List<PriceHistory> PriceHistoryList { get; set; }

  protected int Period { get; set; }

  public AMA(int period) => this.Period = period;

  public override Result Calculate()
  {
    Result result = new Result();
    for (int index1 = 0; index1 < this.PriceHistoryList.Count; ++index1)
    {
      if (index1 >= this.Period)
      {
        double? nullable = result.ResultData[index1 - 1].Value;
        double num1 = Math.Abs(this.PriceHistoryList[index1].Close - this.PriceHistoryList[index1 - this.Period].Close);
        double num2 = 0.0;
        for (int index2 = index1 - this.Period + 1; index2 <= index1; ++index2)
          num2 += Math.Abs(this.PriceHistoryList[index2].Close - this.PriceHistoryList[index2 - 1].Close);
        double sc = AMA.CalculateSC(num2 > 0.0 ? num1 / num2 : 1.0);
        double ama = AMA.CalculateAMA(nullable.Value, this.PriceHistoryList[index1].Close, sc);
        result.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(ama),
          DateTime = this.PriceHistoryList[index1].Date
        });
      }
      else
      {
        double num = this.PriceHistoryList.Take<PriceHistory>(index1 + 1).Select<PriceHistory, double>((Func<PriceHistory, double>) (s => s.Close)).Average();
        result.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(num),
          DateTime = this.PriceHistoryList[index1].Date
        });
      }
    }
    return result;
  }

  private static double CalculateSC(double er) => Math.Pow(er * (56.0 / 93.0) + 2.0 / 31.0, 2.0);

  private static double CalculateAMA(double previousAMA, double currentPrice, double sc)
  {
    return previousAMA + sc * (currentPrice - previousAMA);
  }
}
