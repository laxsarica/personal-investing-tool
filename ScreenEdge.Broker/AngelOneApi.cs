using Newtonsoft.Json;
using ScreenEdge.Broker.Requests;
using ScreenEdge.Broker.Responses;
using RestSharp;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace ScreenEdge.Broker;

public class AngelOneApi
{
    private static RestRequest GetRequest(string clientUrl, string apiKey, Method httpMethod, string accessToken)
    {
        RestRequest request = new RestRequest(clientUrl, httpMethod);
        request.AddHeader("Authorization", "Bearer " + accessToken);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Accept", "application/json");
        request.AddHeader("X-UserType", "USER");
        request.AddHeader("X-SourceID", "WEB");
        request.AddHeader("X-ClientLocalIP", "103.97.46.241");
        request.AddHeader("X-ClientPublicIP", "103.97.46.241");
        request.AddHeader("X-MACAddress", "fe80::216e:6507:4b90:3719");
        request.AddHeader("X-PrivateKey", apiKey);
        return request;
    }

    private static RestRequest CreateRequest(string clientUrl, string apiKey)
    {
        RestRequest request = new RestRequest(clientUrl, Method.Post);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Accept", "application/json");
        request.AddHeader("X-UserType", "USER");
        request.AddHeader("X-SourceID", "WEB");
        request.AddHeader("X-ClientLocalIP", "103.97.46.241");
        request.AddHeader("X-ClientPublicIP", "103.97.46.241");
        request.AddHeader("X-MACAddress", "fe80::216e:6507:4b90:3719");
        request.AddHeader("X-PrivateKey", apiKey);
        return request;
    }

    public static AngelApiResponse<Token> GetLogin(string apiKey, AngelLogin angelLogin)
    {
        RestClient client = new RestClient();
        RestRequest request1 = AngelOneApi.CreateRequest(AngelOneApis.LogingApi, apiKey);
        string str = JsonConvert.SerializeObject((object)angelLogin);
        request1.AddParameter("application/json", (object)str, ParameterType.RequestBody);
        RestRequest request2 = request1;
        return JsonConvert.DeserializeObject<AngelApiResponse<Token>>(client.Execute(request2).Content);
    }

    public static Token GetActiveToken(string apiKey, AngelLogin angelLogin)
    {
        return AngelOneApi.GetLogin(apiKey, angelLogin).data;
    }

    public static long GetTokenExpirationTime(string token)
    {
        return long.Parse(new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.First<Claim>((Func<Claim, bool>)(claim => claim.Type.Equals("exp"))).Value);
    }

    public static bool CheckTokenIsValid(string token)
    {
        return DateTimeOffset.FromUnixTimeSeconds(AngelOneApi.GetTokenExpirationTime(token)).ToUniversalTime() >= (DateTimeOffset)DateTime.Now.ToUniversalTime();
    }

    public static AngelApiResponse<FundDetailResponse> GetFundDetail(
      string apiKey,
      string accessToken)
    {
        return JsonConvert.DeserializeObject<AngelApiResponse<FundDetailResponse>>(new RestClient().Execute(AngelOneApi.GetRequest(AngelOneApis.FundDetail, apiKey, Method.Get, accessToken)).Content);
    }

    public static AngelApiResponse<PlaceOrderResponse> PlaceOrder(
      string apiKey,
      string accessToken,
      PlaceOrderRequest orderRequest)
    {
        RestClient client = new RestClient();
        RestRequest request1 = AngelOneApi.GetRequest(AngelOneApis.PlaceOrder, apiKey, Method.Post, accessToken);
        string str = JsonConvert.SerializeObject((object)orderRequest);
        request1.AddParameter("application/json", (object)str, ParameterType.RequestBody);
        RestRequest request2 = request1;
        return JsonConvert.DeserializeObject<AngelApiResponse<PlaceOrderResponse>>(client.Execute(request2).Content);
    }

    public static AngelApiResponse<ModifyOrderResponse> ModifyOrder(
      string apiKey,
      string accessToken,
      ModifyOrderRequest orderRequest)
    {
        RestClient client = new RestClient();
        RestRequest request1 = AngelOneApi.GetRequest(AngelOneApis.ModifyOrder, apiKey, Method.Post, accessToken);
        string str = JsonConvert.SerializeObject((object)orderRequest);
        request1.AddParameter("application/json", (object)str, ParameterType.RequestBody);
        RestRequest request2 = request1;
        return JsonConvert.DeserializeObject<AngelApiResponse<ModifyOrderResponse>>(client.Execute(request2).Content);
    }

    public static AngelApiResponse<CancelOrderResponse> CancelOrder(
      string apiKey,
      string accessToken,
      CancelOrderRequest orderRequest)
    {
        RestClient client = new RestClient();
        RestRequest request1 = AngelOneApi.GetRequest(AngelOneApis.CancelOrder, apiKey, Method.Post, accessToken);
        string str = JsonConvert.SerializeObject((object)orderRequest);
        request1.AddParameter("application/json", (object)str, ParameterType.RequestBody);
        RestRequest request2 = request1;
        return JsonConvert.DeserializeObject<AngelApiResponse<CancelOrderResponse>>(client.Execute(request2).Content);
    }

    public static AngelApiResponse<List<OrderBookResponse>> GetOrderBook(
      string apiKey,
      string accessToken)
    {
        return JsonConvert.DeserializeObject<AngelApiResponse<List<OrderBookResponse>>>(new RestClient().Execute(AngelOneApi.GetRequest(AngelOneApis.GetOrderBook, apiKey, Method.Get, accessToken)).Content);
    }

    public static AngelApiResponse<List<TradeBookResponse>> GetTradeBook(
      string apiKey,
      string accessToken)
    {
        return JsonConvert.DeserializeObject<AngelApiResponse<List<TradeBookResponse>>>(new RestClient().Execute(AngelOneApi.GetRequest(AngelOneApis.GetTradeBook, apiKey, Method.Get, accessToken)).Content);
    }

    public static AngelApiResponse<LtpDataResponse> GetLtpData(
      string apiKey,
      string accessToken,
      LtpDataRequest dataRequest)
    {
        RestClient client = new RestClient();
        RestRequest request1 = AngelOneApi.GetRequest(AngelOneApis.GetLtpData, apiKey, Method.Post, accessToken);
        string str = JsonConvert.SerializeObject((object)dataRequest);
        request1.AddParameter("application/json", (object)str, ParameterType.RequestBody);
        RestRequest request2 = request1;
        return JsonConvert.DeserializeObject<AngelApiResponse<LtpDataResponse>>(client.Execute(request2).Content);
    }

    public static AngelApiResponse<LiveQuoteResponse> GetLiveQuote(
      string apiKey,
      string accessToken,
      LiveQuoteRequest dataRequest)
    {
        RestClient client = new RestClient();
        RestRequest request1 = AngelOneApi.GetRequest(AngelOneApis.GetLiveQuote, apiKey, Method.Post, accessToken);
        string str = JsonConvert.SerializeObject((object)dataRequest);
        request1.AddParameter("application/json", (object)str, ParameterType.RequestBody);
        RestRequest request2 = request1;
        return JsonConvert.DeserializeObject<AngelApiResponse<LiveQuoteResponse>>(client.Execute(request2).Content);
    }

    public static HistoryDataResponse GetHistoricalData(
      string apiKey,
      string accessToken,
      HistoryDataRequest dataRequest)
    {
        try
        {
            RestClient client = new RestClient();
            RestRequest request1 = AngelOneApi.GetRequest(AngelOneApis.HistoricalData, apiKey, Method.Post, accessToken);
            string str = JsonConvert.SerializeObject((object)dataRequest);
            request1.AddParameter("application/json", (object)str, ParameterType.RequestBody);
            RestRequest request2 = request1;
            return JsonConvert.DeserializeObject<HistoryDataResponse>(client.Execute(request2).Content);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static AngelApiResponse<List<HoldingResponse>> GetHolding(
      string apiKey,
      string accessToken)
    {
        return JsonConvert.DeserializeObject<AngelApiResponse<List<HoldingResponse>>>(new RestClient().Execute(AngelOneApi.GetRequest(AngelOneApis.GetHolding, apiKey, Method.Get, accessToken)).Content);
    }

    public static AngelApiResponse<List<PositionResponse>> GetPositions(
      string apiKey,
      string accessToken)
    {
        return JsonConvert.DeserializeObject<AngelApiResponse<List<PositionResponse>>>(new RestClient().Execute(AngelOneApi.GetRequest(AngelOneApis.GetPosition, apiKey, Method.Get, accessToken)).Content);
    }
}
