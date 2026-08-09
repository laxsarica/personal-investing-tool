using Microsoft.Extensions.Options;
using OtpNet;
using System.Reflection;

namespace ScreenEdge.Broker;

public class TradeApiCreds
{
    private readonly BrokerSettings _settings;

    public TradeApiCreds(IOptions<BrokerSettings> settings)
    {
        _settings = settings.Value;
        this.accessToken = this.RefreshToken();
    }

    public string accessToken { get; set; }
    public string apiKey => _settings.ApiKey;
    
    // Kite Settings
    public string KiteApiKey => _settings.KiteApiKey;
    public string KiteApiSecret => _settings.KiteApiSecret;
    public string KiteAccessToken { get; set; } = string.Empty;

    private string RefreshToken()
    {
        try
        {
            byte[] bytes = Base32Encoding.ToBytes(_settings.TotpSecret);
            var otp = new Totp(bytes).ComputeTotp();
            Token activeToken = AngelOneApi.GetActiveToken(_settings.ApiKey, new AngelLogin()
            {
                password = _settings.Password,
                totp = otp
            });
            var masterDataPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!, "MasterData");
            if (!Directory.Exists(masterDataPath))
            {
                Directory.CreateDirectory(masterDataPath);
            }
            
            File.WriteAllText(Path.Combine(masterDataPath, "token.txt"), activeToken.jwtToken);
            return activeToken.jwtToken;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
