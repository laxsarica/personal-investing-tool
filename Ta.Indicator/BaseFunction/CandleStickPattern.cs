using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ta.Indicator.Base;

namespace Ta.Indicator.BaseFunction;

public class CandleStickPattern
{
    public static string GetPattern(List<PriceHistory> daily)
    {
        string empty = string.Empty;
        List<Candle> candle1 = CandleStickPattern.ConvertToCandle(daily);
        Candle firstCandle = candle1.First<Candle>();
        Candle candle2 = candle1.Skip<Candle>(1).Take<Candle>(1).First<Candle>();
        Candle candle3 = candle1.Last<Candle>();
        if (CandleStickPattern.IsDoji(candle3))
            empty += "Doji,";
        if (CandleStickPattern.IsHammer(candle3))
            empty += "Hammer, ";
        if (CandleStickPattern.IsInvertedHammer(candle3))
            empty += "InvertedHammer, ";
        if (CandleStickPattern.IsBullishEngulfing(candle2, candle3))
            empty += "BullishEngulfing,";
        if (CandleStickPattern.IsPiercingPattern(candle2, candle3))
            empty += "PiercingPattern, ";
        if (CandleStickPattern.IsBullishHarami(candle2, candle3))
            empty += "BullishHarami, ";
        if (CandleStickPattern.IsBullishHaramiCross(candle2, candle3))
            empty += "BullishHarami, ";
        if (CandleStickPattern.IsTweezerBottom(candle2, candle3))
            empty += "TweezerBottom, ";
        if (CandleStickPattern.IsMorningStar(firstCandle, candle2, candle3))
            empty += "MorningStar, ";
        return empty.Trim().TrimEnd(',');
    }
    private static List<Candle> ConvertToCandle(List<PriceHistory> daily)
    {
        List<Candle> candle = new List<Candle>();
        foreach (PriceHistory priceHistory in daily)
            candle.Add(new Candle()
            {
                Open = priceHistory.Open,
                High = priceHistory.High,
                Low = priceHistory.Low,
                Close = priceHistory.Close
            });
        return candle;
    }
    private static bool IsDoji(Candle candle, double threshold = 0.001)
    {
        double num1 = Math.Abs(candle.Open - candle.Close);
        double num2 = candle.High - candle.Low;
        double num3 = threshold * num2;
        return num1 <= num3;
    }
    private static bool IsHammer(Candle candle, double shadowRatio = 2.0)
    {
        double num1 = Math.Abs(candle.Open - candle.Close);
        double num2 = candle.Open > candle.Close ? candle.Open - candle.Low : candle.Close - candle.Low;
        double num3 = candle.High - Math.Max(candle.Open, candle.Close);
        return num1 > 0.0 && num2 >= shadowRatio * num1 && num3 <= num1;
    }
    private static bool IsInvertedHammer(Candle candle, double shadowRatio = 2.0)
    {
        double num1 = Math.Abs(candle.Open - candle.Close);
        double num2 = Math.Min(candle.Open, candle.Close) - candle.Low;
        double num3 = candle.High - Math.Max(candle.Open, candle.Close);
        return num1 > 0.0 && num3 >= shadowRatio * num1 && num2 <= num1;
    }
    private static bool IsBullishEngulfing(Candle firstCandle, Candle secondCandle, double threshold = 0.01)
    {
        int num1 = firstCandle.Close < firstCandle.Open ? 1 : 0;
        bool flag1 = secondCandle.Close > secondCandle.Open;
        bool flag2 = secondCandle.Open <= firstCandle.Close * (1.0 - threshold) && secondCandle.Close >= firstCandle.Open * (1.0 + threshold);
        int num2 = flag1 ? 1 : 0;
        return (num1 & num2 & (flag2 ? 1 : 0)) != 0;
    }

    private static bool IsPiercingPattern(Candle firstCandle, Candle secondCandle, double threshold = 0.01)
    {
        int num1 = firstCandle.Close < firstCandle.Open ? 1 : 0;
        bool flag1 = secondCandle.Close > secondCandle.Open;
        double num2 = (firstCandle.Open + firstCandle.Close) / 2.0;
        bool flag2 = secondCandle.Open < firstCandle.Close * (1.0 - threshold) && secondCandle.Close > num2 * (1.0 - threshold);
        int num3 = flag1 ? 1 : 0;
        return (num1 & num3 & (flag2 ? 1 : 0)) != 0;
    }

    private static bool IsBullishHarami(Candle firstCandle, Candle secondCandle, double threshold = 0.01)
    {
        int num1 = firstCandle.Close < firstCandle.Open ? 1 : 0;
        bool flag1 = secondCandle.Close > secondCandle.Open;
        bool flag2 = secondCandle.Low >= firstCandle.Close * (1.0 + threshold) && secondCandle.High <= firstCandle.Open * (1.0 - threshold);
        int num2 = flag1 ? 1 : 0;
        return (num1 & num2 & (flag2 ? 1 : 0)) != 0;
    }

    private static bool IsBullishHaramiCross(
      Candle firstCandle,
      Candle secondCandle,
      double threshold = 0.01)
    {
        int num1 = firstCandle.Close < firstCandle.Open ? 1 : 0;
        bool flag1 = Math.Abs(secondCandle.Open - secondCandle.Close) <= threshold * (secondCandle.High - secondCandle.Low);
        bool flag2 = secondCandle.Low >= firstCandle.Close * (1.0 - threshold) && secondCandle.High <= firstCandle.Open * (1.0 + threshold);
        int num2 = flag1 ? 1 : 0;
        return (num1 & num2 & (flag2 ? 1 : 0)) != 0;
    }

    private static bool IsTweezerBottom(Candle firstCandle, Candle secondCandle, double threshold = 0.01)
    {
        int num1 = firstCandle.Close < firstCandle.Open ? 1 : 0;
        bool flag1 = secondCandle.Close > secondCandle.Open;
        bool flag2 = Math.Abs(firstCandle.Low - secondCandle.Low) <= threshold * (firstCandle.High - firstCandle.Low);
        int num2 = flag1 ? 1 : 0;
        return (num1 & num2 & (flag2 ? 1 : 0)) != 0;
    }

    private static bool IsMorningStar(
      Candle firstCandle,
      Candle secondCandle,
      Candle thirdCandle,
      double threshold = 0.02)
    {
        int num1 = firstCandle.Close >= firstCandle.Open ? 0 : (CandleStickPattern.IsLargeBody(firstCandle) ? 1 : 0);
        bool flag1 = CandleStickPattern.IsStarCandle(firstCandle, secondCandle, thirdCandle);
        bool flag2 = thirdCandle.Close > thirdCandle.Open && CandleStickPattern.IsLargeBody(thirdCandle);
        bool flag3 = thirdCandle.Close >= (firstCandle.Open + firstCandle.Close) / 2.0;
        int num2 = flag1 ? 1 : 0;
        return (num1 & num2 & (flag2 ? 1 : 0) & (flag3 ? 1 : 0)) != 0;
    }

    private static bool IsLargeBody(Candle candle, double percentageThreshold = 0.025)
    {
        return Math.Abs(candle.Close - candle.Open) >= percentageThreshold * candle.Open;
    }

    private static bool IsSmallBody(Candle candle, double percentageThreshold = 0.02)
    {
        return Math.Abs(candle.Close - candle.Open) <= percentageThreshold * candle.Open;
    }

    private static bool IsSmallWick(Candle candle, double percentageThreshold = 0.04)
    {
        return Math.Abs(candle.High - candle.Low) <= percentageThreshold * candle.Open;
    }

    private static bool IsStarCandle(Candle firstCandle, Candle secondCandle, Candle thirdCandle)
    {
        double num = 0.01;
        return ((!CandleStickPattern.IsSmallBody(secondCandle) ? 0 : (CandleStickPattern.IsSmallWick(secondCandle) ? 1 : 0)) & (secondCandle.Open > (1.0 + num) * firstCandle.Close ? (false ? 1 : 0) : (secondCandle.Close <= (1.0 + num) * firstCandle.Close ? 1 : 0))) != 0;
    }
}
