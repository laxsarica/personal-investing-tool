import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ScreenerService } from '../../../../core/services/screener.service';
import { JobRun } from '../../../../shared/models/screener.model';

@Component({
  selector: 'app-screener-jobs',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './jobs.html',
  styleUrl: './jobs.css'
})
export class JobsComponent implements OnInit {
  jobs: JobRun[] = [];
  loading = false;
  running = false;

  constructor(private screenerService: ScreenerService) {}

  ngOnInit(): void {
    this.loadJobs();
  }

  loadJobs(): void {
    this.loading = true;
    this.screenerService.getJobs().subscribe({
      next: (data: JobRun[]) => {
        this.jobs = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  runScreener(): void {
    this.running = true;
    this.screenerService.runScreener().subscribe({
      next: () => {
        this.running = false;
        this.loadJobs();
      },
      error: () => {
        this.running = false;
      }
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  getBreakdown(job: JobRun): string {
    return job.strategies.map((s: { strategy: string; count: number }) => `${s.strategy.substring(0, 2)}:${s.count}`).join(' ');
  }
}
