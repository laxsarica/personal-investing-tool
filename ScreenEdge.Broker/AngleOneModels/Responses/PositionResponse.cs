namespace ScreenEdge.Broker.Responses;

public class PositionResponse
{
    public string exchange { get; set; } = string.Empty;
    public string tradingsymbol { get; set; } = string.Empty;
    public string symboltoken { get; set; } = string.Empty;
    public string producttype { get; set; } = string.Empty;
    public string duration { get; set; } = string.Empty;
    public decimal buyavgprice { get; set; }
    public decimal sellavgprice { get; set; }
    public string sellqty { get; set; } = string.Empty;
    public string buyqty { get; set; } = string.Empty;
    public int netqty { get; set; }
    public decimal ltp { get; set; }
    public decimal close { get; set; }
    public decimal pnl { get; set; }
    public decimal unrealised { get; set; }
    public decimal realised { get; set; }
}
