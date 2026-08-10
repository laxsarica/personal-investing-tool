using System;
using System.Collections.Generic;
using System.Linq;
using Ta.Indicator.Base;
using TA.Indicators.Indicator;

namespace Ta.CustomIndicator.UptrendBot;

public class UptrendBotIndicator
{
    public int AtrPeriod { get; set; } = 11;
    public double Sensitivity { get; set; } = 2.0;

    public List<UptrendBotResult> Calculate(List<PriceHistory> priceHistories)
    {
        var results = new List<UptrendBotResult>();
        
        if (priceHistories == null || priceHistories.Count < AtrPeriod + 1)
        {
            return results;
        }

        // 1. Calculate ATR for the entire series
        ATR atr = new ATR(AtrPeriod);
        atr.PriceHistoryList = priceHistories;
        var atrData = atr.Calculate().ResultData.ToList();

        double prevStop = 0.0;
        double prevSrc = 0.0;
        int prevPos = 0;

        // Ensure we iterate correctly. atrData count usually matches priceHistories or is slightly offset depending on implementation.
        // For standard Ta.Indicator, they align but the first N elements are null.
        
        for (int i = 0; i < priceHistories.Count; i++)
        {
            var bar = priceHistories[i];
            double src = bar.Close;
            
            // Get ATR value. If it's null (warmup period), we can't calculate yet.
            double? currentAtrOpt = i < atrData.Count ? atrData[i].Value : null;
            if (!currentAtrOpt.HasValue)
            {
                // Just pass through empty values for the warmup
                results.Add(new UptrendBotResult
                {
                    Date = bar.Date,
                    Close = src,
                    TrailingStop = 0,
                    Position = 0,
                    BuySignal = false,
                    SellSignal = false
                });
                prevSrc = src;
                continue;
            }

            double currentAtr = currentAtrOpt.Value;
            double nLoss = Sensitivity * currentAtr;

            double currentStop = 0.0;

            if (src > prevStop && prevSrc > prevStop)
            {
                currentStop = Math.Max(prevStop, src - nLoss);
            }
            else if (src < prevStop && prevSrc < prevStop)
            {
                currentStop = Math.Min(prevStop, src + nLoss);
            }
            else if (src > prevStop)
            {
                currentStop = src - nLoss;
            }
            else
            {
                currentStop = src + nLoss;
            }

            int pos = 0;
            if (prevSrc < prevStop && src > prevStop)
            {
                pos = 1;
            }
            else if (prevSrc > prevStop && src < prevStop)
            {
                pos = -1;
            }
            else
            {
                pos = prevPos;
            }

            // In Pine: buy = src > xATRTrailingStop and above
            // "above" = crossover(ema(src, 1), xATRTrailingStop) 
            // In C#, this crossover happens exactly when prevSrc < prevStop AND src > currentStop.
            // Wait, in Pine it's crossover of src against the *current* stop.
            bool buy = src > currentStop && prevSrc <= prevStop;
            bool sell = src < currentStop && prevSrc >= prevStop;

            results.Add(new UptrendBotResult
            {
                Date = bar.Date,
                Close = src,
                TrailingStop = currentStop,
                Position = pos,
                BuySignal = buy,
                SellSignal = sell
            });

            prevStop = currentStop;
            prevSrc = src;
            prevPos = pos;
        }

        return results;
    }
}
