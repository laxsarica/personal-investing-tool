namespace ScreenEdge.Broker.Responses;
public class OrderBookResponse
{
  public string variety { get; set; }

  public string ordertype { get; set; }

  public string producttype { get; set; }

  public string duration { get; set; }

  public Decimal price { get; set; }

  public Decimal triggerprice { get; set; }

  public int quantity { get; set; }

  public int disclosedquantity { get; set; }

  public Decimal squareoff { get; set; }

  public Decimal stoploss { get; set; }

  public Decimal trailingstoploss { get; set; }

  public string tradingsymbol { get; set; }

  public string transactiontype { get; set; }

  public string exchange { get; set; }

  public int symboltoken { get; set; }

  public string instrumenttype { get; set; }

  public string strikeprice { get; set; }

  public string optiontype { get; set; }

  public DateTime? expirydate { get; set; }

  public int lotsize { get; set; }

  public int cancelsize { get; set; }

  public Decimal averageprice { get; set; }

  public int filledshares { get; set; }

  public int unfilledshares { get; set; }

  public string orderid { get; set; }

  public string text { get; set; }

  public string status { get; set; }

  public string orderstatus { get; set; }

  public DateTime? updatetime { get; set; }

  public DateTime? exchtime { get; set; }

  public DateTime? exchorderupdatetime { get; set; }

  public string fillid { get; set; }

  public string filltime { get; set; }

  public string parentorderid { get; set; }
}
