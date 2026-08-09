import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ScreenerService } from '../../../../core/services/screener.service';
import { ScreenerResult, ScreenerHistoryResponse } from '../../../../shared/models/screener.model';

@Component({
  selector: 'app-screener-history',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './history.html',
  styleUrl: './history.css'
})
export class HistoryComponent implements OnInit {
  results: ScreenerResult[] = [];
  loading = false;
  totalCount = 0;
  page = 1;
  pageSize = 50;

  fromDate = '';
  toDate = '';
  strategy = '';
  symbol = '';

  strategies = ['', 'NOLAG', 'EMAFIFTY', 'SUPPORTRESISTANCE', 'RSIWMA'];

  constructor(private screenerService: ScreenerService) {}

  ngOnInit(): void {
    this.loadHistory();
  }

  loadHistory(): void {
    this.loading = true;
    this.screenerService.getHistory(this.fromDate, this.toDate, this.strategy, this.symbol, this.page, this.pageSize)
      .subscribe({
        next: (data: ScreenerHistoryResponse) => {
          this.results = data.results;
          this.totalCount = data.totalCount;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
        }
      });
  }

  onSearch(): void {
    this.page = 1;
    this.loadHistory();
  }

  nextPage(): void {
    if (this.page * this.pageSize < this.totalCount) {
      this.page++;
      this.loadHistory();
    }
  }

  prevPage(): void {
    if (this.page > 1) {
      this.page--;
      this.loadHistory();
    }
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatPrice(price: number): string {
    return price.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatRsi(rsi: number): string {
    return rsi.toFixed(1);
  }

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }
}
