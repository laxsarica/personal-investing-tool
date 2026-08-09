using Microsoft.AspNetCore.Mvc;
using ScreenEdge.Broker;
using ScreenEdge.Broker.Responses;

namespace ScreenEdge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly IEnumerable<IBrokerPortfolioProvider> _providers;
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(IEnumerable<IBrokerPortfolioProvider> providers, ILogger<PortfolioController> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    private IBrokerPortfolioProvider GetProvider(string broker)
    {
        var provider = _providers.FirstOrDefault(p => string.Equals(p.BrokerName, broker, StringComparison.OrdinalIgnoreCase));
        if (provider == null)
            throw new Exception($"Broker '{broker}' is not supported.");
        return provider;
    }

    /// <summary>Get all equity holdings from the specified broker.</summary>
    [HttpGet("holdings")]
    public async Task<IActionResult> GetHoldings([FromQuery] string broker = "AngelOne")
    {
        try
        {
            var provider = GetProvider(broker);
            var holdings = await provider.GetHoldingsAsync();
            return Ok(holdings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch holdings");
            return Ok(Array.Empty<HoldingResponse>());
        }
    }

    /// <summary>Get all open positions from the specified broker.</summary>
    [HttpGet("positions")]
    public async Task<IActionResult> GetPositions([FromQuery] string broker = "AngelOne")
    {
        try
        {
            var provider = GetProvider(broker);
            var positions = await provider.GetPositionsAsync();
            return Ok(positions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch positions");
            return Ok(Array.Empty<PositionResponse>());
        }
    }

    /// <summary>Get fund/margin details from the specified broker.</summary>
    [HttpGet("funds")]
    public async Task<IActionResult> GetFunds([FromQuery] string broker = "AngelOne")
    {
        try
        {
            var provider = GetProvider(broker);
            var funds = await provider.GetFundsAsync();
            return Ok(funds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch fund details");
            return StatusCode(502, new { message = ex.Message });
        }
    }
}
