namespace ScreenEdge.Broker.Responses;

public class LtpDataResponse
{
  public string exchange { get; set; }

  public string tradingsymbol { get; set; }

  public int symboltoken { get; set; }

  public Decimal open { get; set; }

  public Decimal high { get; set; }

  public Decimal low { get; set; }

  public Decimal close { get; set; }

  public Decimal ltp { get; set; }
}
