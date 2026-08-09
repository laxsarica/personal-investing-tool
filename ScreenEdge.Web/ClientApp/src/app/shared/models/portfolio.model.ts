export interface Holding {
  tradingsymbol: string;
  exchange: string;
  isin: string;
  t1quantity: number;
  realisedquantity: number;
  quantity: number;
  authorisedquantity: number;
  product: string;
  collateraltype: string;
  collateralquantity: number;
  haircut: string;
  averageprice: number;
  ltp: number;
  symboltoken: string;
  close: number;
  profitandloss: number;
  pnlpercentage: number;
}

export interface Position {
  exchange: string;
  tradingsymbol: string;
  symboltoken: string;
  producttype: string;
  duration: string;
  buyavgprice: number;
  sellavgprice: number;
  sellqty: string;
  buyqty: string;
  netqty: number;
  ltp: number;
  close: number;
  pnl: number;
  unrealised: number;
  realised: number;
}

export interface FundDetail {
  net: number;
  availablecash: number;
  availableintradaypayin: number;
  availablelimitmargin: number;
  collateral: number;
  m2munrealized: number;
  m2mrealized: number;
  utiliseddebits: number;
  utilisedspan: number;
  utilisedoptionpremium: number;
  utilisedholdingsales: number;
  utilisedexposure: number;
  utilisedturnover: number;
  utilisedpayout: number;
}
