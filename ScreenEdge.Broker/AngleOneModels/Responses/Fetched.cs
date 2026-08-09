namespace ScreenEdge.Broker.Responses;

public class Fetched
{
  public string exchange { get; set; }

  public string tradingSymbol { get; set; }

  public string symbolToken { get; set; }

  public float ltp { get; set; }

  public float open { get; set; }

  public float high { get; set; }

  public float low { get; set; }

  public float close { get; set; }

  public long tradeVolume { get; set; }

  public DateTime exchFeedTime { get; set; }
}
