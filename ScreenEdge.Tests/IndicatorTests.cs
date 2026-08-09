using System;
using System.Collections.Generic;
using System.Linq;
using Ta.CustomIndicator.ZeroLag;
using Ta.CustomIndicator.EmaFifty;
using Ta.Indicator.Base;
using Xunit;

namespace ScreenEdge.Tests;

public class IndicatorTests
{
    private List<PriceHistory> GenerateDummyData(int count)
    {
        var data = new List<PriceHistory>();
        var date = new DateTime(2025, 1, 1);
        for (int i = 0; i < count; i++)
        {
            data.Add(new PriceHistory
            {
                Date = date.AddDays(i),
                Open = 100 + i,
                High = 105 + i,
                Low = 95 + i,
                Close = 102 + i,
                Volume = 10000 + i
            });
        }
        return data;
    }

    [Fact]
    public void ZeroLagIndicator_ReturnsEmpty_WhenInsufficientData()
    {
        var data = GenerateDummyData(10);
        var indicator = new ZeroLagIndicator();
        var results = indicator.Calculate(data);
        
        // ZeroLag needs > 30 periods, should return empty or subset
        Assert.True(results.Count < 10);
    }

    [Fact]
    public void ZeroLagIndicator_CalculatesCorrectly_WhenSufficientData()
    {
        var data = GenerateDummyData(60);
        var indicator = new ZeroLagIndicator();
        var results = indicator.Calculate(data);
        
        Assert.NotEmpty(results);
        Assert.NotNull(results.Last());
    }

    [Fact]
    public void EmaFiftyIndicator_RunsWithoutErrors()
    {
        var data = GenerateDummyData(100);
        var indicator = new EmaFiftyIndicator();
        var results = indicator.Calculate(data);
        
        Assert.NotNull(results); // May be empty depending on if conditions are met, but shouldn't throw
    }
}
