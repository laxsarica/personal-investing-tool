using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ScreenEdge.Broker.Kite;

public static class KiteApi
{
    private const string BaseUrl = "https://api.kite.trade";

    private static RestRequest GetRequest(string endpoint, string apiKey, string accessToken)
    {
        var request = new RestRequest(endpoint, Method.Get);
        request.AddHeader("X-Kite-Version", "3");
        request.AddHeader("Authorization", $"token {apiKey}:{accessToken}");
        return request;
    }

    public static KiteResponse<List<KiteHolding>> GetHoldings(string apiKey, string accessToken)
    {
        var client = new RestClient(BaseUrl);
        var request = GetRequest("/portfolio/holdings", apiKey, accessToken);
        var response = client.Execute(request);
        if (response.IsSuccessful && response.Content != null)
        {
            return JsonConvert.DeserializeObject<KiteResponse<List<KiteHolding>>>(response.Content);
        }
        return null;
    }

    public static KiteResponse<KitePositionData> GetPositions(string apiKey, string accessToken)
    {
        var client = new RestClient(BaseUrl);
        var request = GetRequest("/portfolio/positions", apiKey, accessToken);
        var response = client.Execute(request);
        if (response.IsSuccessful && response.Content != null)
        {
            return JsonConvert.DeserializeObject<KiteResponse<KitePositionData>>(response.Content);
        }
        return null;
    }

    public static KiteResponse<KiteMarginData> GetMargins(string apiKey, string accessToken)
    {
        var client = new RestClient(BaseUrl);
        var request = GetRequest("/user/margins", apiKey, accessToken);
        var response = client.Execute(request);
        if (response.IsSuccessful && response.Content != null)
        {
            return JsonConvert.DeserializeObject<KiteResponse<KiteMarginData>>(response.Content);
        }
        return null;
    }

    public static KiteTokenResponse GetAccessToken(string apiKey, string apiSecret, string requestToken)
    {
        var client = new RestClient(BaseUrl);
        var request = new RestRequest("/session/token", Method.Post);
        request.AddHeader("X-Kite-Version", "3");
        request.AddParameter("api_key", apiKey, ParameterType.GetOrPost);
        request.AddParameter("request_token", requestToken, ParameterType.GetOrPost);
        
        string checksumInput = apiKey + requestToken + apiSecret;
        string checksum = ComputeSha256Hash(checksumInput);
        Console.WriteLine($"[KiteApi] Checksum input: api_key({apiKey.Length} chars) + request_token({requestToken.Length} chars) + api_secret({apiSecret.Length} chars) = {checksumInput.Length} chars total");
        Console.WriteLine($"[KiteApi] Checksum result: {checksum}");
        request.AddParameter("checksum", checksum, ParameterType.GetOrPost);

        var response = client.Execute(request);
        
        // Log the raw response for debugging
        System.Diagnostics.Debug.WriteLine($"Kite /session/token HTTP {(int)response.StatusCode}: {response.Content}");
        Console.WriteLine($"[KiteApi] /session/token HTTP {(int)response.StatusCode}: {response.Content}");
        
        if (response.Content != null)
        {
            try
            {
                return JsonConvert.DeserializeObject<KiteTokenResponse>(response.Content);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    private static string ComputeSha256Hash(string rawData)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
