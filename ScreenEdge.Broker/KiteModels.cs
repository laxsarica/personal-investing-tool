using System.Collections.Generic;

namespace ScreenEdge.Broker.Kite;

public class KiteResponse<T>
{
    public string status { get; set; }
    public T data { get; set; }
}

public class KiteHolding
{
    public string tradingsymbol { get; set; }
    public string exchange { get; set; }
    public long quantity { get; set; }
    public long t1_quantity { get; set; }
    public double average_price { get; set; }
    public double last_price { get; set; }
    public double close_price { get; set; }
    public double pnl { get; set; }
    public double day_change { get; set; }
    public double day_change_percentage { get; set; }
}

public class KitePositionData
{
    public List<KitePosition> net { get; set; }
    public List<KitePosition> day { get; set; }
}

public class KitePosition
{
    public string tradingsymbol { get; set; }
    public string exchange { get; set; }
    public string product { get; set; }
    public long quantity { get; set; }
    public double average_price { get; set; }
    public double buy_price { get; set; }
    public double sell_price { get; set; }
    public double last_price { get; set; }
    public double close_price { get; set; }
    public double pnl { get; set; }
    public double realised { get; set; }
    public double unrealised { get; set; }
}

public class KiteMarginData
{
    public KiteMarginSegment equity { get; set; }
    public KiteMarginSegment commodity { get; set; }
}

public class KiteMarginSegment
{
    public bool enabled { get; set; }
    public double net { get; set; }
    public KiteMarginAvailable available { get; set; }
    public KiteMarginUtilised utilised { get; set; }
}

public class KiteMarginAvailable
{
    public double cash { get; set; }
    public double opening_balance { get; set; }
    public double live_balance { get; set; }
    public double collateral { get; set; }
}

public class KiteMarginUtilised
{
    public double debits { get; set; }
    public double exposure { get; set; }
    public double m2m_realised { get; set; }
    public double m2m_unrealised { get; set; }
    public double span { get; set; }
}

public class KiteTokenResponse
{
    public string status { get; set; }
    public KiteTokenData data { get; set; }
}

public class KiteTokenData
{
    public string access_token { get; set; }
    public string public_token { get; set; }
}
