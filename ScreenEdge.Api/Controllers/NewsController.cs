using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScreenEdge.Entity;

namespace ScreenEdge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NewsController> _logger;

    public NewsController(IHttpClientFactory httpClientFactory, ILogger<NewsController> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    /// <summary>Fetch stock news from TradingView public API.</summary>
    [HttpGet("{symbol}")]
    public async Task<IActionResult> GetNews(string symbol)
    {
        try
        {
            var tvSymbol = $"NSE:{symbol.ToUpperInvariant()}";
            var encoded = Uri.EscapeDataString($"symbol:{tvSymbol}");
            var url = $"https://news-mediator.tradingview.com/public/view/v1/symbol?filter=lang%3Aen&filter={encoded}&client=landing&streaming=false&user_prostatus=non_pro";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return Ok(new { items = Array.Empty<object>() });

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching news for {Symbol}", symbol);
            return Ok(new { items = Array.Empty<object>() });
        }
    }
}
