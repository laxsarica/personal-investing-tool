namespace ScreenEdge.Broker.Responses;

public class TradeBookResponse
{
  public string exchange { get; set; }

  public string producttype { get; set; }

  public string tradingsymbol { get; set; }

  public string instrumenttype { get; set; }

  public string symbolgroup { get; set; }

  public Decimal strikeprice { get; set; }

  public string optiontype { get; set; }

  public DateTime expirydate { get; set; }

  public int marketlot { get; set; }

  public int precision { get; set; }

  public int multiplier { get; set; }

  public Decimal tradevalue { get; set; }

  public string transactiontype { get; set; }

  public Decimal fillprice { get; set; }

  public int fillsize { get; set; }

  public string orderid { get; set; }

  public string fillid { get; set; }

  public DateTime filltime { get; set; }
}
