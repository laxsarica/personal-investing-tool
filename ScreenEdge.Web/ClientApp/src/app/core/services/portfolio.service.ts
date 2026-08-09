import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Holding, Position, FundDetail } from '../../shared/models/portfolio.model';

@Injectable({ providedIn: 'root' })
export class PortfolioService {
  private readonly API_URL = 'http://localhost:5100/api/portfolio';

  constructor(private http: HttpClient) {}

  getHoldings(broker: string = 'AngelOne'): Observable<Holding[]> {
    return this.http.get<Holding[]>(`${this.API_URL}/holdings?broker=${broker}`);
  }

  getPositions(broker: string = 'AngelOne'): Observable<Position[]> {
    return this.http.get<Position[]>(`${this.API_URL}/positions?broker=${broker}`);
  }

  getFunds(broker: string = 'AngelOne'): Observable<FundDetail> {
    return this.http.get<FundDetail>(`${this.API_URL}/funds?broker=${broker}`);
  }

  getKiteStatus(): Observable<{isAuthenticated: boolean}> {
    return this.http.get<{isAuthenticated: boolean}>('http://localhost:5100/api/kiteauth/status');
  }
}
