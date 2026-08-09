namespace ScreenEdge.Broker.Requests;

public class HistoryDataRequest
{
  public string exchange { get; set; }

  public string symboltoken { get; set; }

  public string interval { get; set; }

  public string fromdate { get; set; }

  public string todate { get; set; }

  public HistoryDataRequest()
  {
    DateTime dateTime = DateTime.Now;
    dateTime = dateTime.AddDays(-1.0);
    this.fromdate= dateTime.ToString("yyyy-MM-dd HH:mm");
    this.todate= DateTime.Now.ToString("yyyy-MM-dd HH:mm");
  }
}
