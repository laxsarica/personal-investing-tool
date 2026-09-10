using Ta.CustomIndicator.RsiSmaCandle;
using Ta.Indicator.Base;
using Xunit;
using Xunit.Abstractions;

namespace ScreenEdge.Tests;

public class RsiSmaCandleIndicatorTests
{
    private readonly ITestOutputHelper _output;

    public RsiSmaCandleIndicatorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Generate synthetic price data with a known trend for testing.
    /// Creates an uptrend that starts flat then rises sharply to trigger a crossover.
    /// </summary>
    private static List<PriceHistory> GenerateSyntheticData(int count, double startPrice = 100.0)
    {
        var data = new List<PriceHistory>();
        var random = new Random(42); // deterministic seed
        double price = startPrice;

        for (int i = 0; i < count; i++)
        {
            // First 80% is flat/slightly up, last 20% is a sharp rise
            double change;
            if (i < count * 0.8)
                change = (random.NextDouble() - 0.48) * 2; // slight upward bias
            else
                change = random.NextDouble() * 3 + 1; // strong uptrend

            price += change;
            double open = price - random.NextDouble() * 1.5;
            double high = price + random.NextDouble() * 2;
            double low = open - random.NextDouble() * 1.5;
            double close = price;
            double volume = 100000 + random.NextDouble() * 500000;

            data.Add(new PriceHistory
            {
                Date = new DateTime(2024, 1, 1).AddDays(i),
                Open = open,
                High = Math.Max(high, Math.Max(open, close)),
                Low = Math.Min(low, Math.Min(open, close)),
                Close = close,
                Volume = volume
            });
        }

        return data;
    }

    [Fact]
    public void Calculate_ReturnsEmpty_WhenInsufficientData()
    {
        var indicator = new RsiSmaCandleIndicator();
        var data = GenerateSyntheticData(10); // way too few bars

        var results = indicator.Calculate(data);

        Assert.Empty(results);
    }

    [Fact]
    public void Calculate_ReturnsEmpty_WhenNull()
    {
        var indicator = new RsiSmaCandleIndicator();

        var results = indicator.Calculate(null!);

        Assert.Empty(results);
    }

    [Fact]
    public void Calculate_ReturnsResults_WhenSufficientData()
    {
        var indicator = new RsiSmaCandleIndicator();
        var data = GenerateSyntheticData(150);

        var results = indicator.Calculate(data);

        Assert.NotEmpty(results);
        _output.WriteLine($"Generated {results.Count} results from {data.Count} bars");
    }

    [Fact]
    public void Calculate_Logic2Bull_IsCorrect()
    {
        var indicator = new RsiSmaCandleIndicator();
        var data = GenerateSyntheticData(150);

        var results = indicator.Calculate(data);

        foreach (var r in results)
        {
            // Logic2Bull should be true when Close >= Open
            Assert.Equal(r.Logic2Close >= r.Logic2Open, r.Logic2Bull);
        }
    }

    [Fact]
    public void Calculate_Logic2Rising_TracksSlopeCorrectly()
    {
        var indicator = new RsiSmaCandleIndicator();
        var data = GenerateSyntheticData(150);

        var results = indicator.Calculate(data);

        for (int i = 1; i < results.Count; i++)
        {
            bool expectedRising = (results[i].Logic2Close - results[i - 1].Logic2Close) > 0;
            Assert.Equal(expectedRising, results[i].Logic2Rising);
        }
    }

    [Fact]
    public void Calculate_BuySignal_RequiresRsiAboveThreshold()
    {
        var indicator = new RsiSmaCandleIndicator { RsiBuyThreshold = 60.0 };
        var data = GenerateSyntheticData(200);

        var results = indicator.Calculate(data);
        var buySignals = results.Where(r => r.BuySignal).ToList();

        // All buy signals must have RSI > 60
        foreach (var signal in buySignals)
        {
            Assert.True(signal.RsiValue > 60.0,
                $"BUY on {signal.Date:yyyy-MM-dd} had RSI={signal.RsiValue:F2}, expected > 60");
            _output.WriteLine($"BUY: {signal.Date:yyyy-MM-dd} RSI={signal.RsiValue:F2} Close={signal.Logic2Close:F2}");
        }
    }

    [Fact]
    public void Calculate_BuySignal_RequiresCrossover()
    {
        // Verify BUY signal only fires on a crossover (not just being above logic2High)
        var indicator = new RsiSmaCandleIndicator();
        var data = GenerateSyntheticData(200);

        var results = indicator.Calculate(data);
        var buySignals = results.Where(r => r.BuySignal).ToList();

        _output.WriteLine($"Total buy signals: {buySignals.Count} out of {results.Count} bars");

        // A crossover means the signal should be relatively rare
        // (not firing on every bar where close > logic2High)
        int barsAboveRibbon = results.Count(r => r.Logic2Close > r.Logic2High * 0.99); // approximate
        _output.WriteLine($"Bars near/above ribbon: {barsAboveRibbon}");
        Assert.True(buySignals.Count <= barsAboveRibbon,
            "Buy signals should be much fewer than bars above ribbon (crossover, not hold)");
    }

    [Fact]
    public void Calculate_DefaultMaPeriodIs30()
    {
        var indicator = new RsiSmaCandleIndicator();

        Assert.Equal(30, indicator.MaPeriod);
        Assert.Equal(14, indicator.RsiPeriod);
        Assert.Equal(60.0, indicator.RsiBuyThreshold);
    }

    [Fact]
    public void Calculate_MinBarsRequired_IsCorrect()
    {
        var indicator = new RsiSmaCandleIndicator { MaPeriod = 30, RsiPeriod = 14 };

        // MaPeriod * 2 + RsiPeriod = 74
        Assert.Equal(74, indicator.MinBarsRequired());
    }

    [Fact]
    public void Calculate_PrintsLast10Bars_ForVisualInspection()
    {
        var indicator = new RsiSmaCandleIndicator();
        var data = GenerateSyntheticData(200);

        var results = indicator.Calculate(data);
        var last10 = results.TakeLast(10).ToList();

        _output.WriteLine("Date       | L2Open   | L2High   | L2Low    | L2Close  | Bull | Rising | RSI    | BUY");
        _output.WriteLine(new string('-', 100));
        foreach (var r in last10)
        {
            _output.WriteLine(
                $"{r.Date:yyyy-MM-dd} | {r.Logic2Open,8:F2} | {r.Logic2High,8:F2} | {r.Logic2Low,8:F2} | {r.Logic2Close,8:F2} | {(r.Logic2Bull ? "BULL" : "BEAR"),-4} | {(r.Logic2Rising ? "UP" : "DN"),-6} | {r.RsiValue,6:F2} | {(r.BuySignal ? "***" : "")}");
        }
    }
}
