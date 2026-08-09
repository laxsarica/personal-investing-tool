import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-kite-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="main-content" style="display:flex; align-items:center; justify-content:center; height:80vh;">
      <div style="text-align:center;">
        @if (error) {
          <div style="color:var(--loss); font-weight:600;">{{ error }}</div>
          <button class="btn btn-primary" style="margin-top: var(--space-4);" (click)="goToPortfolio()">Back to Portfolio</button>
        } @else {
          <div style="color:var(--text-secondary);">
            <i class="ri-loader-4-line" style="font-size: 24px;"></i>
            Authenticating with Kite...
          </div>
        }
      </div>
    </div>
  `
})
export class KiteCallbackComponent implements OnInit {
  error = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    const params = this.route.snapshot.queryParams;
    const requestToken = params['request_token'];
    const status = params['status'];

    if (status === 'success' && requestToken) {
      this.http.post<{isAuthenticated: boolean}>('http://localhost:5100/api/kiteauth/exchange-token', {
        requestToken: requestToken
      }).subscribe({
        next: (res) => {
          if (res.isAuthenticated) {
            this.router.navigate(['/portfolio']);
          } else {
            this.error = 'Token exchange failed. Please try again.';
          }
        },
        error: (err) => {
          this.error = err.error?.message || 'Failed to authenticate with Kite.';
        }
      });
    } else {
      this.error = 'Kite login was cancelled or failed.';
    }
  }

  goToPortfolio(): void {
    this.router.navigate(['/portfolio']);
  }
}
