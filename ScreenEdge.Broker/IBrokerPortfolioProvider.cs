using System.Collections.Generic;
using System.Threading.Tasks;
using ScreenEdge.Broker.Responses;

namespace ScreenEdge.Broker;

public interface IBrokerPortfolioProvider
{
    string BrokerName { get; }
    Task<List<HoldingResponse>> GetHoldingsAsync();
    Task<List<PositionResponse>> GetPositionsAsync();
    Task<FundDetailResponse> GetFundsAsync();
}
