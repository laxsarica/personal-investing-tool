import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/auth/login/login').then(m => m.LoginComponent) },
  {
    path: 'kite-callback',
    loadComponent: () => import('./features/portfolio/kite-callback').then(m => m.KiteCallbackComponent)
  },
  {
    path: 'screener',
    canActivate: [authGuard],
    children: [
      { path: '', loadComponent: () => import('./features/screener/pages/dashboard/dashboard').then(m => m.DashboardComponent) },
      { path: 'history', loadComponent: () => import('./features/screener/pages/history/history').then(m => m.HistoryComponent) },
      { path: 'jobs', loadComponent: () => import('./features/screener/pages/jobs/jobs').then(m => m.JobsComponent) },
    ]
  },
  {
    path: 'portfolio',
    canActivate: [authGuard],
    loadComponent: () => import('./features/portfolio/portfolio').then(m => m.PortfolioComponent)
  },
  { path: '', redirectTo: '/screener', pathMatch: 'full' },
  { path: '**', redirectTo: '/screener' }
];

