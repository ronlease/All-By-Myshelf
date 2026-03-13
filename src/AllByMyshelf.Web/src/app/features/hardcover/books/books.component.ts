import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FeaturesDto, FeaturesService } from '../../../core/config/features.service';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatToolbarModule } from '@angular/material/toolbar';
import { HttpErrorResponse } from '@angular/common/http';
import { BookDto, HardcoverService } from '../hardcover.service';

@Component({
  selector: 'app-books',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTableModule,
    MatToolbarModule,
    RouterModule,
  ],
  templateUrl: './books.component.html',
})
export class BooksComponent implements OnInit {
  books = signal<BookDto[]>([]);
  currentPage = signal(1);
  readonly displayedColumns = ['thumbnail', 'author', 'title', 'genre', 'year'];
  features = signal<FeaturesDto | null>(null);
  private readonly featuresService = inject(FeaturesService);
  private readonly hardcoverService = inject(HardcoverService);
  loading = signal(true);
  readonly pageSize = 25;
  private readonly snackBar = inject(MatSnackBar);
  syncing = signal(false);
  private syncTimer?: ReturnType<typeof setTimeout>;
  totalCount = signal(0);

  loadBooks(page: number): void {
    this.loading.set(true);
    this.hardcoverService.getBooks(page, this.pageSize).subscribe({
      next: (result) => {
        this.books.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
        this.syncing.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.syncing.set(false);
        this.snackBar.open('Failed to load books.', 'Dismiss', { duration: 5000 });
      },
    });
  }

  ngOnInit(): void {
    this.featuresService.getFeatures().subscribe({
      next: (f) => this.features.set(f),
    });
    this.loadBooks(this.currentPage());
  }

  onPageChange(event: PageEvent): void {
    this.currentPage.set(event.pageIndex + 1);
    this.loadBooks(this.currentPage());
  }

  onSyncClick(): void {
    if (this.syncing()) return;
    this.syncing.set(true);
    this.hardcoverService.triggerSync().subscribe({
      next: (response) => {
        if (response.status === 202) {
          this.snackBar.open('Sync started.', 'Dismiss', { duration: 3000 });
          this.pollSyncStatus();
        } else {
          this.syncing.set(false);
        }
      },
      error: (err: HttpErrorResponse) => {
        this.syncing.set(false);
        if (err.status === 409) {
          this.snackBar.open('A sync is already in progress.', 'Dismiss', { duration: 5000 });
        } else if (err.status === 503) {
          this.snackBar.open('Hardcover API key is not configured.', 'Dismiss', { duration: 5000 });
        } else {
          this.snackBar.open('An unexpected error occurred.', 'Dismiss', { duration: 5000 });
        }
      },
    });
  }

  private pollSyncStatus(): void {
    const poll = () => {
      this.hardcoverService.getSyncStatus().subscribe({
        next: (status) => {
          if (status.isRunning) {
            this.syncTimer = setTimeout(poll, 2000);
          } else {
            this.syncing.set(false);
            this.loadBooks(this.currentPage());
          }
        },
        error: () => {
          this.syncTimer = setTimeout(poll, 3000);
        },
      });
    };
    poll();
  }
}
