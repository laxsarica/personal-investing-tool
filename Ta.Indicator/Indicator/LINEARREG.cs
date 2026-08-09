using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;

namespace TA.Indicators.Indicator;

public class LINEARREG : IndicatorCalculatorBase<Result>
{
  public override List<PriceHistory> PriceHistoryList { get; set; }

  protected int Period { get; set; }

  public LINEARREG(int period) => this.Period = period;

  public override Result Calculate()
  {
    Result PriceHistoryList = new Result();
    for (int index1 = 0; index1 < this.PriceHistoryList.Count; ++index1)
    {
      List<double> doubleList = new List<double>();
      if (index1 >= this.Period)
      {
        for (int index2 = index1; index2 >= index1 - (this.Period - 1); --index2)
          doubleList.Add(this.PriceHistoryList[index2].Close);
        double linearRegression = LINEARREG.CalculateLinearRegression(doubleList.ToArray(), 11, 0);
        PriceHistoryList.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(linearRegression),
          DateTime = this.PriceHistoryList[index1].Date
        });
      }
      else
        PriceHistoryList.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(0.0),
          DateTime = this.PriceHistoryList[index1].Date
        });
    }
    return LINEARREG.CalculateSma(11, PriceHistoryList);
  }

  public static double CalculateLinearRegression(double[] x, int length, int offset)
  {
    if (x.Length < length)
      throw new ArgumentException("Input array length must be greater than or equal to the specified length.");
    int num1 = length;
    double num2 = 0.0;
    double num3 = 0.0;
    double num4 = 0.0;
    double num5 = 0.0;
    for (int index = 0; index < num1; ++index)
    {
      num2 += (double) index;
      num3 += x[index];
      num4 += (double) index * x[index];
      num5 += (double) (index * index);
    }
    double num6 = ((double) num1 * num4 - num2 * num3) / ((double) num1 * num5 - num2 * num2);
    return (num3 - num6 * num2) / (double) num1 + num6 * (double) (length - 1 - offset);
  }

  private static Result CalculateSma(int Period, Result PriceHistoryList)
  {
    Result sma = new Result();
    List<TimeSeriesData> resultData = PriceHistoryList.ResultData;
    for (int index1 = 0; index1 < resultData.Count; ++index1)
    {
      if (index1 >= Period - 1)
      {
        double num1 = 0.0;
        for (int index2 = index1; index2 >= index1 - (Period - 1); --index2)
          num1 += resultData[index2].Value.Value;
        double num2 = num1 / (double) Period;
        sma.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(num2),
          DateTime = resultData[index1].DateTime
        });
      }
      else
        sma.ResultData.Add(new TimeSeriesData()
        {
          Value = new double?(0.0),
          DateTime = resultData[index1].DateTime
        });
    }
    return sma;
  }
}
