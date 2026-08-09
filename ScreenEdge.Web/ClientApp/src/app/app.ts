import { Component, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class AppComponent implements OnInit {
  constructor(public authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    // Intercept Kite redirect: http://localhost:4200?request_token=xxx&status=success
    const params = new URLSearchParams(window.location.search);
    const requestToken = params.get('request_token');
    const status = params.get('status');
    if (requestToken && status) {
      this.router.navigate(['/kite-callback'], {
        queryParams: { request_token: requestToken, status: status }
      });
    }
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
