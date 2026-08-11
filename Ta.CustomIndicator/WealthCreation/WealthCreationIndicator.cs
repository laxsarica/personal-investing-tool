using System;
using System.Collections.Generic;
using System.Linq;
using Ta.Indicator.Base;
using Ta.Indicator.BaseFunction;
using Ta.Indicator.Indicator;
using TA.Indicators.Indicator;

namespace Ta.CustomIndicator.WealthCreation;

public class WealthCreationIndicator
{
    public int RsiPeriod { get; set; } = 14;
    public int Ema50Period { get; set; } = 50;
    public int Ema200Period { get; set; } = 200;
    public int VolMaPeriod { get; set; } = 20;

    public List<WealthCreationResult> Calculate(List<PriceHistory> dailyData)
    {
        var results = new List<WealthCreationResult>();
        
        if (dailyData == null || dailyData.Count < Ema200Period)
        {
            return results;
        }

        var dailyEma50Series = new EMA(Ema50Period) { PriceHistoryList = dailyData }.Calculate().ResultData;
        var dailyEma200Series = new EMA(Ema200Period) { PriceHistoryList = dailyData }.Calculate().ResultData;
        var dailyVolumeMa20Series = new VolumeMA(VolMaPeriod) { PriceHistoryList = dailyData }.Calculate().ResultData;

        var weeklyData = DataConverter.ConvertToWeeklyOHLC(dailyData);
        var weeklyRsiSeries = new RSI(RsiPeriod) { PriceHistoryList = weeklyData }.Calculate().ResultData;

        // Map weekly RSI values back to the exact Date of the weekly candle close
        var weeklyRsiDict = new Dictionary<DateTime, double>();
        for (int j = 1; j < weeklyRsiSeries.Count; j++)
        {
            if (j < weeklyData.Count && weeklyRsiSeries[j] != null && weeklyRsiSeries[j].Value.HasValue)
            {
                weeklyRsiDict[weeklyData[j].Date] = weeklyRsiSeries[j].Value.Value;
            }
        }

        double? previousWeeklyRsi = null;

        for (int i = 0; i < dailyData.Count; i++)
        {
            var date = dailyData[i].Date;
            var close = dailyData[i].Close;
            
            double currentWeeklyRsi = 0;
            bool hasWeeklyRsi = weeklyRsiDict.TryGetValue(date, out currentWeeklyRsi);
            
            double ema50 = dailyEma50Series.Count > i && dailyEma50Series[i] != null && dailyEma50Series[i].Value.HasValue ? dailyEma50Series[i].Value.Value : 0;
            double ema200 = dailyEma200Series.Count > i && dailyEma200Series[i] != null && dailyEma200Series[i].Value.HasValue ? dailyEma200Series[i].Value.Value : 0;
            double volMa20 = dailyVolumeMa20Series.Count > i && dailyVolumeMa20Series[i] != null && dailyVolumeMa20Series[i].Value.HasValue ? dailyVolumeMa20Series[i].Value.Value : 0;

            bool buySignal = false;
            bool sellSignal = false;

            if (hasWeeklyRsi)
            {
                bool isUptrend = ema200 > 0 && close > ema200;
                bool isHighVolume = dailyData[i].Volume > volMa20;

                // Entry condition: crossed 60 + uptrend + high volume
                if (previousWeeklyRsi.HasValue && previousWeeklyRsi.Value <= 60 && currentWeeklyRsi > 60 && isUptrend && isHighVolume)
                {
                    buySignal = true;
                }
                
                previousWeeklyRsi = currentWeeklyRsi;
            }

            // Exit condition
            if (ema50 > 0 && close < ema50)
            {
                sellSignal = true;
            }

            results.Add(new WealthCreationResult
            {
                Date = date,
                Close = close,
                WeeklyRsi = currentWeeklyRsi,
                Ema50 = ema50,
                Ema200 = ema200,
                VolMa20 = volMa20,
                BuySignal = buySignal,
                SellSignal = sellSignal
            });
        }

        return results;
    }
}
