import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { LoginRequest, LoginResponse } from '../../shared/models/screener.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly API_URL = 'http://localhost:5100/api/auth';
  private readonly TOKEN_KEY = 'screenedge_token';
  private readonly USERNAME_KEY = 'screenedge_username';

  private loggedIn = new BehaviorSubject<boolean>(this.hasToken());

  get isLoggedIn(): boolean {
    return this.hasToken();
  }

  get username(): string {
    return localStorage.getItem(this.USERNAME_KEY) || '';
  }

  get token(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  constructor(private http: HttpClient) {}

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.API_URL}/login`, request).pipe(
      tap(response => {
        localStorage.setItem(this.TOKEN_KEY, response.token);
        localStorage.setItem(this.USERNAME_KEY, response.username);
        this.loggedIn.next(true);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USERNAME_KEY);
    this.loggedIn.next(false);
  }

  private hasToken(): boolean {
    return !!localStorage.getItem(this.TOKEN_KEY);
  }
}
