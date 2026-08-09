namespace ScreenEdge.Broker.Requests;

public class LiveQuoteRequest
{
  public LiveQuoteRequest() => this.exchangeTokens = new Exchangetokens();

  public string mode { get; set; } = "OHLC";

  public Exchangetokens exchangeTokens { get; set; }
}
