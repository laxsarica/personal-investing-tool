using System.Runtime.InteropServices;
using Ta.Indicator.Base;
using Ta.Indicator.Indicator;
using TA.Indicators.Indicator;

namespace Ta.CustomIndicator.EmaFifty;

public class EmaFiftyIndicator
{
    public int EmaLength { get; set; } = 50;
    public int VolumeLength { get; set; } = 20;

    public List<double> Prices { get; set; } = new List<double>();
    public List<double> Volumes { get; set; } = new List<double>();

    public List<EmaFiftyResult> Calculate(List<PriceHistory> priceHistories)
    {
        List<EmaFiftyResult> emaFiftyResultList = new List<EmaFiftyResult>();
        if (priceHistories.Count > 50)
        {
            EMA ema = new EMA(this.EmaLength);
            ema.PriceHistoryList = priceHistories;
            double num1 = ema.Calculate().ResultData.Last().Value.Value;

            VolumeMA vma = new VolumeMA(this.VolumeLength);
            vma.PriceHistoryList = priceHistories;
            double num2 = vma.Calculate().ResultData.Last().Value.Value;

            var crossType = this.CheckPriceEmaCrossover(priceHistories.TakeLast(2).ToList(), num1);
            var isVolumeSpikeBy20Percent = priceHistories.Last().Volume > (num2 * 1.2);
            if (crossType == CrossType.Bullish && isVolumeSpikeBy20Percent)
            {
                emaFiftyResultList.Add(new EmaFiftyResult()
                {
                    Date = priceHistories.Last().Date,
                    UpSignal = true,
                });
            }
        }

        return emaFiftyResultList;
    }


    public CrossType CheckPriceEmaCrossover(List<PriceHistory> prices, double emaValues)
    {

        var prevPrice = (prices.First().Open + prices.First().Close) / 2;
        var currPrice = (prices.Last().Open + prices.Last().Close) / 2;

        // Detect crossing
        if (prevPrice < emaValues && currPrice > emaValues)
            return CrossType.Bullish;  // Price crossed above EMA

        if (prevPrice > emaValues && currPrice < emaValues)
            return CrossType.Bearish;  // Price crossed below EMA

        return CrossType.None;
    }

}
