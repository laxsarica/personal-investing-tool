namespace ScreenEdge.Broker.Responses;

public class HoldingResponse
{
    public string tradingsymbol { get; set; } = string.Empty;
    public string exchange { get; set; } = string.Empty;
    public string isin { get; set; } = string.Empty;
    public int t1quantity { get; set; }
    public int realisedquantity { get; set; }
    public int quantity { get; set; }
    public int authorisedquantity { get; set; }
    public string product { get; set; } = string.Empty;
    public string collateraltype { get; set; } = string.Empty;
    public int collateralquantity { get; set; }
    public string haircut { get; set; } = string.Empty;
    public decimal averageprice { get; set; }
    public decimal ltp { get; set; }
    public string symboltoken { get; set; } = string.Empty;
    public decimal close { get; set; }
    public decimal profitandloss { get; set; }
    public decimal pnlpercentage { get; set; }
}
