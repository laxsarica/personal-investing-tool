
namespace ScreenEdge.Broker;

public class AngelApiResponse<T>
{
    public bool status { get; set; }

    public string message { get; set; }

    public string errorcode { get; set; }

    public T data { get; set; }
}
public class Token
{
    public string jwtToken { get; set; }

    public string refreshToken { get; set; }

    public string feedToken { get; set; }
}
public class AngelLogin
{
    public string clientcode { get; set; } = "AABT692203";

    public string password { get; set; } = "Vivek@9aug";

    public string totp { get; set; } = "606250";
}
public class LastTradingPriceReqest
{
    public string exchange { get; set; } = "NSE";

    public string tradingsymbol { get; set; }

    public string symboltoken { get; set; }
}

public class LastTradingPrice
{
    public string exchange { get; set; }

    public string tradingsymbol { get; set; }

    public string symboltoken { get; set; }

    public Decimal open { get; set; }

    public Decimal high { get; set; }

    public Decimal low { get; set; }

    public Decimal close { get; set; }

    public Decimal ltp { get; set; }
}
