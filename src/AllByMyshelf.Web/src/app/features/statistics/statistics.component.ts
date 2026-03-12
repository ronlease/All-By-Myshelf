import { Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatToolbarModule } from '@angular/material/toolbar';
import { CollectionValueDto, StatisticsService } from './statistics.service';

@Component({
  selector: 'app-statistics',
  standalone: true,
  imports: [
    CurrencyPipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatToolbarModule,
  ],
  templateUrl: './statistics.component.html',
})
export class StatisticsComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly statisticsService = inject(StatisticsService);

  collectionValue = signal<CollectionValueDto | null>(null);
  error = signal(false);
  loading = signal(true);

  ngOnInit(): void {
    this.statisticsService.getCollectionValue().subscribe({
      next: (data) => {
        this.collectionValue.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  onBackClick(): void {
    this.router.navigate(['/']);
  }
}
