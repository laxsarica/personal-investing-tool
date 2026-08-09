namespace ScreenEdge.Broker;

public static class AngelOneApis
{
    public static string LogingApi { get; } = "https://apiconnect.angelbroking.com/rest/auth/angelbroking/user/v1/loginByPassword";

    public static string FundDetail { get; } = "https://apiconnect.angelbroking.com/rest/secure/angelbroking/user/v1/getRMS";

    public static string PlaceOrder { get; } = "https://apiconnect.angelbroking.com/rest/secure/angelbroking/order/v1/placeOrder";

    public static string ModifyOrder { get; } = "https://apiconnect.angelbroking.com/rest/secure/angelbroking/order/v1/modifyOrder";

    public static string CancelOrder { get; } = "https://apiconnect.angelbroking.com/rest/secure/angelbroking/order/v1/cancelOrder";

    public static string GetOrderBook { get; } = "https://apiconnect.angelbroking.com/rest/secure/angelbroking/order/v1/getOrderBook";

    public static string GetTradeBook { get; } = "https://apiconnect.angelbroking.com/rest/secure/angelbroking/order/v1/getTradeBook";

    public static string GetLtpData { get; } = "https://apiconnect.angelbroking.com/rest/secure/angelbroking/order/v1/getLtpData";

    public static string GetLiveQuote { get; } = "https://apiconnect.angelbroking.com/rest/secure/angelbroking/market/v1/quote/";

    public static string HistoricalData { get; } = "https://apiconnect.angelbroking.com/rest/secure/angelbroking/historical/v1/getCandleData";

    public static string GetHolding { get; } = "https://apiconnect.angelbroking.com/rest/secure/angelbroking/portfolio/v1/getHolding";

    public static string GetPosition { get; } = "https://apiconnect.angelbroking.com/rest/secure/angelbroking/order/v1/getPosition";
}
