export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  username: string;
}

export interface ScreenerResult {
  id: number;
  symbol: string;
  screenerName: string;
  timeFrame: string;
  recognizeDate: string;
  rsi: number;
  rsiWeekly: number;
  rsiMonthly: number;
  volume: number;
  recognizedPrice: number;
  marketCapCategory: string | null;
}

export interface StockFundamental {
  id: number;
  peRatio: number | null;
  pbRatio: number | null;
  dividendYield: number | null;
  fiftyTwoWeekHigh: number | null;
  fiftyTwoWeekLow: number | null;
  industry: string;
  website: string;
  description: string;
  lastUpdated: string;
}

export interface ScreenerHistoryResponse {
  totalCount: number;
  page: number;
  pageSize: number;
  results: ScreenerResult[];
}

export interface JobRun {
  runDate: string;
  totalSignals: number;
  strategies: { strategy: string; count: number }[];
}

export interface ScreenerJobResult {
  timeMinutes: number;
  recordCount: number;
  totalStocksScanned: number;
  signalsByStrategy: { [key: string]: number };
  status: string;
  errors: string[];
}

export interface StockNewsProvider {
  id: string;
  name: string;
  logo_id: string;
}

export interface StockNewsRelatedSymbol {
  symbol: string;
  logoid?: string;
}

export interface StockNewsItem {
  id: string;
  title: string;
  published: number; // unix timestamp
  paywall: boolean;
  link?: string;
  storyPath?: string;
  provider: StockNewsProvider;
  relatedSymbols: StockNewsRelatedSymbol[];
}
