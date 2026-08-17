import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ScreenerResult, ScreenerHistoryResponse, JobRun, ScreenerJobResult, StockFundamental, StockNewsItem } from '../../shared/models/screener.model';

@Injectable({ providedIn: 'root' })
export class ScreenerService {
  private readonly API_URL = 'http://localhost:5100/api/screener';

  constructor(private http: HttpClient) {}

  runScreener(): Observable<ScreenerJobResult> {
    return this.http.post<ScreenerJobResult>(`${this.API_URL}/run-all`, {});
  }

  getResults(strategy?: string, timeFrame?: string): Observable<ScreenerResult[]> {
    let params = new HttpParams();
    if (strategy) params = params.set('strategy', strategy);
    if (timeFrame) params = params.set('timeFrame', timeFrame);
    return this.http.get<ScreenerResult[]>(`${this.API_URL}/results`, { params });
  }

  getHistory(from?: string, to?: string, strategy?: string, symbol?: string, page = 1, pageSize = 50): Observable<ScreenerHistoryResponse> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    if (strategy) params = params.set('strategy', strategy);
    if (symbol) params = params.set('symbol', symbol);
    return this.http.get<ScreenerHistoryResponse>(`${this.API_URL}/results/history`, { params });
  }

  getJobs(): Observable<JobRun[]> {
    return this.http.get<JobRun[]>(`${this.API_URL}/jobs`);
  }

  getStockFundamentals(symbol: string): Observable<StockFundamental> {
    return this.http.get<StockFundamental>(`${this.API_URL}/fundamentals/${symbol}`);
  }

  getStockNews(symbol: string): Observable<{ items: StockNewsItem[] }> {
    return this.http.get<{ items: StockNewsItem[] }>(`http://localhost:5100/api/news/${symbol}`);
  }
}
