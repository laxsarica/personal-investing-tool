using System.Collections.Generic;
using System.Threading.Tasks;
using ScreenEdge.Broker.Responses;
using System;

namespace ScreenEdge.Broker;

public class AngelOnePortfolioProvider : IBrokerPortfolioProvider
{
    private readonly TradeApiCreds _creds;

    public AngelOnePortfolioProvider(TradeApiCreds creds)
    {
        _creds = creds;
    }

    public string BrokerName => "AngelOne";

    public Task<List<HoldingResponse>> GetHoldingsAsync()
    {
        var response = AngelOneApi.GetHolding(_creds.apiKey, _creds.accessToken);
        if (response == null || !response.status || response.data == null)
            return Task.FromResult(new List<HoldingResponse>());
        
        return Task.FromResult(response.data);
    }

    public Task<List<PositionResponse>> GetPositionsAsync()
    {
        var response = AngelOneApi.GetPositions(_creds.apiKey, _creds.accessToken);
        if (response == null || !response.status || response.data == null)
            return Task.FromResult(new List<PositionResponse>());
        
        return Task.FromResult(response.data);
    }

    public Task<FundDetailResponse> GetFundsAsync()
    {
        var response = AngelOneApi.GetFundDetail(_creds.apiKey, _creds.accessToken);
        if (response == null || !response.status || response.data == null)
            throw new Exception("Failed to fetch fund details from AngelOne");
            
        return Task.FromResult(response.data);
    }
}
