using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScreenEdge.Broker.Responses;

namespace ScreenEdge.Broker.Kite;

public class KitePortfolioProvider : IBrokerPortfolioProvider
{
    private readonly TradeApiCreds _creds;

    public KitePortfolioProvider(TradeApiCreds creds)
    {
        _creds = creds;
    }

    public string BrokerName => "Kite";

    public Task<List<HoldingResponse>> GetHoldingsAsync()
    {
        var response = KiteApi.GetHoldings(_creds.KiteApiKey, _creds.KiteAccessToken);
        if (response == null || response.status != "success" || response.data == null)
            return Task.FromResult(new List<HoldingResponse>());

        var mappedHoldings = response.data.Select(k => new HoldingResponse
        {
            tradingsymbol = k.tradingsymbol,
            exchange = k.exchange,
            quantity = (int)k.quantity,
            t1quantity = (int)k.t1_quantity,
            averageprice = (decimal)k.average_price,
            ltp = (decimal)k.last_price,
            close = (decimal)k.close_price,
            profitandloss = (decimal)k.pnl,
            pnlpercentage = (decimal)k.day_change_percentage
        }).ToList();

        return Task.FromResult(mappedHoldings);
    }

    public Task<List<PositionResponse>> GetPositionsAsync()
    {
        var response = KiteApi.GetPositions(_creds.KiteApiKey, _creds.KiteAccessToken);
        if (response == null || response.status != "success" || response.data == null || response.data.net == null)
            return Task.FromResult(new List<PositionResponse>());

        var mappedPositions = response.data.net.Select(k => new PositionResponse
        {
            tradingsymbol = k.tradingsymbol,
            exchange = k.exchange,
            producttype = k.product,
            netqty = (int)k.quantity,
            buyavgprice = (decimal)k.buy_price,
            sellavgprice = (decimal)k.sell_price,
            ltp = (decimal)k.last_price,
            close = (decimal)k.close_price,
            pnl = (decimal)k.pnl,
            realised = (decimal)k.realised,
            unrealised = (decimal)k.unrealised
        }).ToList();

        return Task.FromResult(mappedPositions);
    }

    public Task<FundDetailResponse> GetFundsAsync()
    {
        var response = KiteApi.GetMargins(_creds.KiteApiKey, _creds.KiteAccessToken);
        if (response == null || response.status != "success" || response.data == null)
            throw new Exception("Failed to fetch fund details from Kite");

        var equity = response.data.equity;
        
        var mappedFunds = new FundDetailResponse
        {
            net = (decimal)(equity?.net ?? 0),
            availablecash = (decimal)(equity?.available?.cash ?? 0),
            collateral = (decimal)(equity?.available?.collateral ?? 0),
            m2mrealized = (decimal)(equity?.utilised?.m2m_realised ?? 0),
            m2munrealized = (decimal)(equity?.utilised?.m2m_unrealised ?? 0),
            utiliseddebits = (decimal)(equity?.utilised?.debits ?? 0),
            utilisedspan = (decimal)(equity?.utilised?.span ?? 0),
            utilisedexposure = (decimal)(equity?.utilised?.exposure ?? 0)
        };

        return Task.FromResult(mappedFunds);
    }
}
