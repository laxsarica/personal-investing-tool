namespace ScreenEdge.Broker.Responses;

public class LiveQuoteResponse
{
  public Fetched[] fetched { get; set; }

  public object[] unfetched { get; set; }
}
