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
