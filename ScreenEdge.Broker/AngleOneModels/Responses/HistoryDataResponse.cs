namespace ScreenEdge.Broker.Responses;

public class HistoryDataResponse
{
  public bool status { get; set; }

  public string message { get; set; }

  public string errorcode { get; set; }

  public object[][] data { get; set; }
}
