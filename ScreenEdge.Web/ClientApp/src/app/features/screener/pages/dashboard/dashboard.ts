import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ScreenerService } from '../../../../core/services/screener.service';
import { ScreenerResult, StockFundamental, StockNewsItem } from '../../../../shared/models/screener.model';

@Component({
  selector: 'app-screener-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DashboardComponent implements OnInit {
  results: ScreenerResult[] = [];
  filteredResults: ScreenerResult[] = [];
  loading = false;
  activeStrategy = 'All';
  activeTimeFrame = 'All';
  sortColumn = 'symbol';
  sortDirection: 'asc' | 'desc' = 'asc';
  currentTime: string = '';

  // Highlight sets — computed from full results
  multiTimeFrameSymbols = new Set<string>();
  multiStrategySymbols = new Set<string>();

  // RSI range filters
  rsiMin: number | null = null;
  rsiMax: number | null = null;
  rsiWeeklyMin: number | null = null;
  rsiWeeklyMax: number | null = null;
  rsiMonthlyMin: number | null = null;
  rsiMonthlyMax: number | null = null;

  expandedSections: { [key: string]: boolean } = {
    strategy: true,
    timeframe: true,
    marketCap: true,
    rsi: false,
    rsiWeekly: false,
    rsiMonthly: false
  };

  marketCapFilters = {
    'LargeCap': false,
    'MidCap': false,
    'SmallCap': false,
    'MicroCap': false
  };

  isSlideOutOpen = false;
  selectedStockSymbol = '';
  selectedStockDetail: StockFundamental | null = null;
  loadingDetails = false;
  activeTab: 'fundamentals' | 'news' = 'fundamentals';
  stockNews: StockNewsItem[] = [];
  loadingNews = false;

  strategies = ['All', 'NOLAG', 'EMAFIFTY', 'SUPPORTRESISTANCE', 'RSIWMA', 'UPTRENDBOT'];
  timeFrames = ['All', 'D', 'W'];

  constructor(private screenerService: ScreenerService) {}

  ngOnInit(): void {
    this.loadResults();
  }

  loadResults(): void {
    this.loading = true;
    this.screenerService.getResults().subscribe({
      next: (data: ScreenerResult[]) => {
        this.results = data;
        this.computeHighlights(data);
        this.applyFilters();
        
        const now = new Date();
        this.currentTime = now.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
        
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  private computeHighlights(data: ScreenerResult[]): void {
    // Group by symbol
    const bySymbol = new Map<string, ScreenerResult[]>();
    for (const r of data) {
      if (!bySymbol.has(r.symbol)) bySymbol.set(r.symbol, []);
      bySymbol.get(r.symbol)!.push(r);
    }

    this.multiTimeFrameSymbols.clear();
    this.multiStrategySymbols.clear();

    bySymbol.forEach((rows, symbol) => {
      const timeFrames = new Set(rows.map(r => r.timeFrame));
      if (timeFrames.has('D') && timeFrames.has('W')) {
        this.multiTimeFrameSymbols.add(symbol);
      }
      const strategies = new Set(rows.map(r => r.screenerName));
      if (strategies.size > 1) {
        this.multiStrategySymbols.add(symbol);
      }
    });
  }

  toggleSection(section: string): void {
    this.expandedSections[section] = !this.expandedSections[section];
  }

  setStrategy(strategy: string): void {
    this.activeStrategy = strategy;
    this.applyFilters();
  }

  toggleMarketCap(cap: 'LargeCap' | 'MidCap' | 'SmallCap' | 'MicroCap'): void {
    this.marketCapFilters[cap] = !this.marketCapFilters[cap];
    this.applyFilters();
  }

  resetFilters(): void {
    this.activeStrategy = 'All';
    this.activeTimeFrame = 'All';
    this.marketCapFilters = { 'LargeCap': false, 'MidCap': false, 'SmallCap': false, 'MicroCap': false };
    this.rsiMin = null; this.rsiMax = null;
    this.rsiWeeklyMin = null; this.rsiWeeklyMax = null;
    this.rsiMonthlyMin = null; this.rsiMonthlyMax = null;
    this.applyFilters();
  }

  setTimeFrame(tf: string): void {
    this.activeTimeFrame = tf;
    this.applyFilters();
  }

  setRsiPreset(field: 'rsi' | 'rsiWeekly' | 'rsiMonthly', preset: 'low' | 'mid' | 'high' | 'reset'): void {
    const ranges: Record<string, [number | null, number | null]> = {
      low:   [0, 40],
      mid:   [40, 60],
      high:  [60, 100],
      reset: [null, null]
    };
    const [min, max] = ranges[preset];
    if (field === 'rsi')        { this.rsiMin = min;        this.rsiMax = max; }
    if (field === 'rsiWeekly')  { this.rsiWeeklyMin = min;  this.rsiWeeklyMax = max; }
    if (field === 'rsiMonthly') { this.rsiMonthlyMin = min; this.rsiMonthlyMax = max; }
    this.applyFilters();
  }

  isPreset(field: 'rsi' | 'rsiWeekly' | 'rsiMonthly', preset: 'low' | 'mid' | 'high'): boolean {
    const presets: Record<string, [number, number]> = { low: [0, 40], mid: [40, 60], high: [60, 100] };
    const [min, max] = presets[preset];
    if (field === 'rsi')        return this.rsiMin === min && this.rsiMax === max;
    if (field === 'rsiWeekly')  return this.rsiWeeklyMin === min && this.rsiWeeklyMax === max;
    if (field === 'rsiMonthly') return this.rsiMonthlyMin === min && this.rsiMonthlyMax === max;
    return false;
  }

  sort(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
    this.applyFilters();
  }

  private applyFilters(): void {
    let data = [...this.results];

    if (this.activeStrategy !== 'All') {
      data = data.filter(r => r.screenerName === this.activeStrategy);
    }
    if (this.activeTimeFrame !== 'All') {
      data = data.filter(r => r.timeFrame === this.activeTimeFrame);
    }
    if (this.rsiMin !== null)        data = data.filter(r => r.rsi >= this.rsiMin!);
    if (this.rsiMax !== null)        data = data.filter(r => r.rsi <= this.rsiMax!);
    if (this.rsiWeeklyMin !== null)  data = data.filter(r => r.rsiWeekly >= this.rsiWeeklyMin!);
    if (this.rsiWeeklyMax !== null)  data = data.filter(r => r.rsiWeekly <= this.rsiWeeklyMax!);
    if (this.rsiMonthlyMin !== null) data = data.filter(r => r.rsiMonthly >= this.rsiMonthlyMin!);
    if (this.rsiMonthlyMax !== null) data = data.filter(r => r.rsiMonthly <= this.rsiMonthlyMax!);

    const mCapSelected = Object.values(this.marketCapFilters).some(v => v);
    if (mCapSelected) {
      data = data.filter(r => {
        if (!r.marketCapCategory) return false;
        
        // Exact string match on the backend-provided category
        if (this.marketCapFilters['LargeCap'] && r.marketCapCategory === 'LargeCap') return true;
        if (this.marketCapFilters['MidCap'] && r.marketCapCategory === 'MidCap') return true;
        if (this.marketCapFilters['SmallCap'] && r.marketCapCategory === 'SmallCap') return true;
        if (this.marketCapFilters['MicroCap'] && r.marketCapCategory === 'MicroCap') return true;
        
        return false;
      });
    }

    data.sort((a, b) => {
      const aVal = (a as any)[this.sortColumn];
      const bVal = (b as any)[this.sortColumn];
      const cmp = typeof aVal === 'string' ? aVal.localeCompare(bVal) : aVal - bVal;
      return this.sortDirection === 'asc' ? cmp : -cmp;
    });

    this.filteredResults = data;
  }

  formatVolume(vol: number): string {
    if (vol >= 10000000) return (vol / 10000000).toFixed(1) + 'Cr';
    if (vol >= 100000) return (vol / 100000).toFixed(1) + 'L';
    if (vol >= 1000) return (vol / 1000).toFixed(1) + 'K';
    return vol.toString();
  }

  formatPrice(price: number): string {
    return price.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatRsi(rsi: number): string {
    return rsi.toFixed(1);
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  openSlideOut(symbol: string): void {
    this.selectedStockSymbol = symbol;
    this.selectedStockDetail = null;
    this.stockNews = [];
    this.isSlideOutOpen = true;
    this.activeTab = 'fundamentals';
    this.loadingDetails = true;
    this.loadingNews = true;

    this.screenerService.getStockFundamentals(symbol).subscribe({
      next: (details) => {
        this.selectedStockDetail = details;
        this.loadingDetails = false;
      },
      error: () => {
        this.loadingDetails = false;
      }
    });

    this.screenerService.getStockNews(symbol).subscribe({
      next: (resp) => {
        this.stockNews = resp.items || [];
        this.loadingNews = false;
      },
      error: () => {
        this.loadingNews = false;
      }
    });
  }

  setTab(tab: 'fundamentals' | 'news'): void {
    this.activeTab = tab;
  }

  formatNewsDate(unixTs: number): string {
    return new Date(unixTs * 1000).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  closeSlideOut(): void {
    this.isSlideOutOpen = false;
  }
}
