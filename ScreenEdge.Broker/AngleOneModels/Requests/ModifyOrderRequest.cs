namespace ScreenEdge.Broker.Requests;

public class ModifyOrderRequest
{
  public string variety { get; set; }

  public string orderid { get; set; }

  public string ordertype { get; set; }

  public string producttype { get; set; }

  public string duration { get; set; }

  public Decimal price { get; set; }

  public int quantity { get; set; }

  public string tradingsymbol { get; set; }

  public int symboltoken { get; set; }

  public string exchange { get; set; }
}
