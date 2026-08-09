import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PortfolioService } from '../../core/services/portfolio.service';
import { Holding, Position, FundDetail } from '../../shared/models/portfolio.model';

@Component({
  selector: 'app-portfolio',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './portfolio.html',
  styleUrl: './portfolio.css'
})
export class PortfolioComponent implements OnInit {
  activeTab: 'holdings' | 'positions' | 'funds' = 'holdings';
  selectedBroker: string = 'AngelOne';

  holdings: Holding[] = [];
  positions: Position[] = [];
  funds: FundDetail | null = null;

  loadingHoldings = false;
  loadingPositions = false;
  loadingFunds = false;
  isKiteAuthenticated = false;

  constructor(private portfolioService: PortfolioService) {}

  ngOnInit(): void {
    this.checkKiteStatus();
    this.loadAll();
  }

  checkKiteStatus(): void {
    if (this.selectedBroker === 'Kite') {
      this.portfolioService.getKiteStatus().subscribe({
        next: (res) => this.isKiteAuthenticated = res.isAuthenticated,
        error: () => this.isKiteAuthenticated = false
      });
    }
  }

  loadAll(): void {
    this.loadHoldings();
    this.loadPositions();
    this.loadFunds();
  }

  loadHoldings(): void {
    this.loadingHoldings = true;
    this.portfolioService.getHoldings(this.selectedBroker).subscribe({
      next: (data) => { this.holdings = data; this.loadingHoldings = false; },
      error: () => { this.loadingHoldings = false; }
    });
  }

  loadPositions(): void {
    this.loadingPositions = true;
    this.portfolioService.getPositions(this.selectedBroker).subscribe({
      next: (data) => { this.positions = data; this.loadingPositions = false; },
      error: () => { this.loadingPositions = false; }
    });
  }

  loadFunds(): void {
    this.loadingFunds = true;
    this.portfolioService.getFunds(this.selectedBroker).subscribe({
      next: (data) => { this.funds = data; this.loadingFunds = false; },
      error: () => { this.loadingFunds = false; }
    });
  }

  setTab(tab: 'holdings' | 'positions' | 'funds'): void {
    this.activeTab = tab;
  }

  setBroker(broker: string): void {
    this.selectedBroker = broker;
    this.checkKiteStatus();
    this.loadAll();
  }

  loginToKite(): void {
    window.location.href = 'http://localhost:5000/api/kiteauth/login';
  }

  get totalHoldingInvested(): number {
    return this.holdings.reduce((sum, h) => sum + h.averageprice * h.quantity, 0);
  }

  get totalHoldingCurrent(): number {
    return this.holdings.reduce((sum, h) => sum + h.ltp * h.quantity, 0);
  }

  get totalHoldingPnl(): number {
    return this.holdings.reduce((sum, h) => sum + h.profitandloss, 0);
  }

  get totalPositionPnl(): number {
    return this.positions.reduce((sum, p) => sum + p.pnl, 0);
  }

  formatCurrency(val: number): string {
    return (val ?? 0).toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatPct(val: number): string {
    return val.toFixed(2) + '%';
  }
}
