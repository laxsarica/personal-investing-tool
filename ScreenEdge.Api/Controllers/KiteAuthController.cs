using Microsoft.AspNetCore.Mvc;
using ScreenEdge.Broker;
using ScreenEdge.Broker.Kite;

namespace ScreenEdge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KiteAuthController : ControllerBase
{
    private readonly TradeApiCreds _creds;
    private readonly ILogger<KiteAuthController> _logger;

    public KiteAuthController(TradeApiCreds creds, ILogger<KiteAuthController> logger)
    {
        _creds = creds;
        _logger = logger;
    }

    /// <summary>Returns the Kite login URL as JSON (frontend opens it).</summary>
    [HttpGet("login-url")]
    public IActionResult GetLoginUrl()
    {
        string loginUrl = $"https://kite.zerodha.com/connect/login?v=3&api_key={_creds.KiteApiKey}";
        return Ok(new { loginUrl });
    }

    /// <summary>Redirects browser to Kite login (legacy / direct).</summary>
    [HttpGet("login")]
    public IActionResult Login()
    {
        string loginUrl = $"https://kite.zerodha.com/connect/login?v=3&api_key={_creds.KiteApiKey}";
        return Redirect(loginUrl);
    }

    /// <summary>
    /// Frontend calls this with the request_token it received from Kite's redirect.
    /// Exchanges the request_token for an access_token and stores it.
    /// </summary>
    [HttpPost("exchange-token")]
    public IActionResult ExchangeToken([FromBody] KiteExchangeTokenRequest request)
    {
        _logger.LogInformation("ExchangeToken called with request_token={RequestToken}", request.RequestToken);

        if (string.IsNullOrEmpty(request.RequestToken))
            return BadRequest(new { message = "request_token is required." });

        try
        {
            var response = KiteApi.GetAccessToken(_creds.KiteApiKey, _creds.KiteApiSecret, request.RequestToken);
            _logger.LogInformation("Kite token exchange response: status={Status}", response?.status ?? "null");

            if (response != null && response.status == "success" && response.data != null)
            {
                _creds.KiteAccessToken = response.data.access_token;
                _logger.LogInformation("Successfully stored Kite access token.");
                return Ok(new { isAuthenticated = true });
            }
            else
            {
                _logger.LogError("Kite token exchange failed — response was null or status != success.");
                return BadRequest(new { message = "Failed to authenticate with Kite. Token exchange returned an error." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Kite token exchange.");
            return StatusCode(500, new { message = "Error exchanging token: " + ex.Message });
        }
    }

    /// <summary>Callback from Kite with the request_token (if redirect URL points here).</summary>
    [HttpGet("callback")]
    public IActionResult Callback([FromQuery] string request_token, [FromQuery] string action, [FromQuery] string status)
    {
        _logger.LogInformation("Kite Callback received: request_token={RequestToken}, action={Action}, status={Status}", request_token, action, status);

        if (status == "success" && !string.IsNullOrEmpty(request_token))
        {
            try
            {
                var response = KiteApi.GetAccessToken(_creds.KiteApiKey, _creds.KiteApiSecret, request_token);
                if (response != null && response.status == "success" && response.data != null)
                {
                    _creds.KiteAccessToken = response.data.access_token;
                    _logger.LogInformation("Successfully retrieved Kite access token via callback.");
                    return Redirect("http://localhost:4200/portfolio");
                }
                else
                {
                    _logger.LogError("Failed to get access token from Kite API via callback.");
                    return BadRequest("Failed to authenticate with Kite.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Kite token exchange via callback.");
                return StatusCode(500, "Error exchanging token.");
            }
        }

        return BadRequest("Invalid callback parameters.");
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        bool isAuthenticated = !string.IsNullOrEmpty(_creds.KiteAccessToken);
        return Ok(new { isAuthenticated });
    }
}

public class KiteExchangeTokenRequest
{
    public string RequestToken { get; set; } = string.Empty;
}
