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

    private string _accessToken = string.Empty;
    private readonly object _lock = new();

    public string accessToken
    {
        get
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(_accessToken) || !IsTokenValid(_accessToken))
                {
                    _accessToken = RefreshToken();
                }
                return _accessToken;
            }
        }
        set
        {
            lock (_lock)
            {
                _accessToken = value;
            }
        }
    }

    public string apiKey => _settings.ApiKey;
    
    // Kite Settings
    public string KiteApiKey => _settings.KiteApiKey;
    public string KiteApiSecret => _settings.KiteApiSecret;
    public string KiteAccessToken { get; set; } = string.Empty;

    private static bool IsTokenValid(string token)
    {
        try
        {
            return AngelOneApi.CheckTokenIsValid(token);
        }
        catch
        {
            return false;
        }
    }

    public string RefreshToken()
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

            if (activeToken == null || string.IsNullOrEmpty(activeToken.jwtToken))
            {
                return string.Empty;
            }

            var entryLoc = Assembly.GetEntryAssembly()?.Location;
            if (!string.IsNullOrEmpty(entryLoc))
            {
                var masterDataPath = Path.Combine(Path.GetDirectoryName(entryLoc)!, "MasterData");
                if (!Directory.Exists(masterDataPath))
                {
                    Directory.CreateDirectory(masterDataPath);
                }
                File.WriteAllText(Path.Combine(masterDataPath, "token.txt"), activeToken.jwtToken);
            }

            return activeToken.jwtToken;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
