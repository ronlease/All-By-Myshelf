import { Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { StatisticsService, UnifiedStatisticsDto } from './statistics.service';

@Component({
  selector: 'app-statistics',
  standalone: true,
  imports: [
    CurrencyPipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatListModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './statistics.component.html',
  styleUrl: './statistics.component.scss',
})
export class StatisticsComponent implements OnInit {
  error = signal(false);
  loading = signal(true);
  statistics = signal<UnifiedStatisticsDto | null>(null);
  private readonly statisticsService = inject(StatisticsService);

  ngOnInit(): void {
    this.statisticsService.getAll().subscribe({
      next: (data) => {
        this.statistics.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  retry(): void {
    this.error.set(false);
    this.loading.set(true);
    this.ngOnInit();
  }
}
