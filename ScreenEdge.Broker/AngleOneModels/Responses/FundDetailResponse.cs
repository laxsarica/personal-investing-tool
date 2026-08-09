namespace ScreenEdge.Broker.Responses;
public class FundDetailResponse
{
  public decimal? net { get; set; }

  public decimal? availablecash { get; set; }

  public decimal? availableintradaypayin { get; set; }

  public decimal? availablelimitmargin { get; set; }

  public decimal? collateral { get; set; }

  public decimal? m2munrealized { get; set; }

  public decimal? m2mrealized { get; set; }

  public decimal? utiliseddebits { get; set; }

  public decimal? utilisedspan { get; set; }

  public decimal? utilisedoptionpremium { get; set; }

  public decimal? utilisedholdingsales { get; set; }

  public decimal? utilisedexposure { get; set; }

  public decimal? utilisedturnover { get; set; }

  public decimal? utilisedpayout { get; set; }
}
