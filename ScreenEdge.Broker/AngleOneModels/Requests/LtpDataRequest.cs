namespace ScreenEdge.Broker.Requests;

public class LtpDataRequest
{
  public Guid subscriberId { get; set; }

  public string exchange { get; set; }

  public string tradingsymbol { get; set; }

  public int symboltoken { get; set; }
}
