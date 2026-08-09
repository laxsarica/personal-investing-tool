namespace ScreenEdge.Broker;

/// <summary>
/// Broker API credentials — bound from configuration section "BrokerSettings".
/// Register in DI via: builder.Services.Configure&lt;BrokerSettings&gt;(builder.Configuration.GetSection("BrokerSettings"))
/// Inject with: IOptions&lt;BrokerSettings&gt;
/// </summary>
public class BrokerSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string TotpSecret { get; set; } = string.Empty;
    public string KiteApiKey { get; set; } = string.Empty;
    public string KiteApiSecret { get; set; } = string.Empty;
}
