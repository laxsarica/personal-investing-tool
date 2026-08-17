using System.Threading.Tasks;
using YahooFinanceApi;

namespace ScreenEdge.Api.Services;

public class YahooFinanceFundamentalDto
{
    public decimal? MarketCap { get; set; }
    public decimal? PeRatio { get; set; }
    public decimal? PbRatio { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? FiftyTwoWeekHigh { get; set; }
    public decimal? FiftyTwoWeekLow { get; set; }
}

public class YahooFinanceService
{
    public async Task<YahooFinanceFundamentalDto?> GetFundamentalsAsync(string symbol)
    {
        // Append .NS if it doesn't have it
        var yahooSymbol = symbol.EndsWith(".NS", StringComparison.OrdinalIgnoreCase) ? symbol : $"{symbol}.NS";

        try
        {
            var result = await Yahoo.Symbols(yahooSymbol)
                .Fields(
                    Field.MarketCap,
                    Field.TrailingPE,
                    Field.PriceToBook,
                    Field.TrailingAnnualDividendYield,
                    Field.FiftyTwoWeekHigh,
                    Field.FiftyTwoWeekLow
                )
                .QueryAsync();

            if (result == null || !result.ContainsKey(yahooSymbol))
                return null;

            var quote = result[yahooSymbol];
            var dto = new YahooFinanceFundamentalDto();

            if (quote.Fields.ContainsKey("MarketCap"))
                dto.MarketCap = (decimal?)quote[Field.MarketCap];

            if (quote.Fields.ContainsKey("TrailingPE"))
                dto.PeRatio = (decimal?)quote[Field.TrailingPE];

            if (quote.Fields.ContainsKey("PriceToBook"))
                dto.PbRatio = (decimal?)quote[Field.PriceToBook];

            if (quote.Fields.ContainsKey("TrailingAnnualDividendYield"))
                dto.DividendYield = (decimal?)quote[Field.TrailingAnnualDividendYield] * 100;

            if (quote.Fields.ContainsKey("FiftyTwoWeekHigh"))
                dto.FiftyTwoWeekHigh = (decimal?)quote[Field.FiftyTwoWeekHigh];

            if (quote.Fields.ContainsKey("FiftyTwoWeekLow"))
                dto.FiftyTwoWeekLow = (decimal?)quote[Field.FiftyTwoWeekLow];

            return dto;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
