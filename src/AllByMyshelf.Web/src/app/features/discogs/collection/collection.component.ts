import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatCardModule } from '@angular/material/card';
import { HttpErrorResponse } from '@angular/common/http';
import { DiscogsService, ReleaseDto, PagedResult } from '../discogs.service';

@Component({
  selector: 'app-collection',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatButtonModule,
    MatSnackBarModule,
    MatToolbarModule,
    MatCardModule,
  ],
  templateUrl: './collection.component.html',
})
export class CollectionComponent implements OnInit {
  private readonly discogsService = inject(DiscogsService);
  private readonly snackBar = inject(MatSnackBar);

  readonly displayedColumns = ['artist', 'title', 'year', 'format'];
  readonly pageSize = 20;

  loading = signal(true);
  syncing = signal(false);
  currentPage = signal(1);
  pagedResult = signal<PagedResult<ReleaseDto> | null>(null);

  get releases(): ReleaseDto[] {
    return this.pagedResult()?.items ?? [];
  }

  get totalCount(): number {
    return this.pagedResult()?.totalCount ?? 0;
  }

  ngOnInit(): void {
    this.loadPage(1);
  }

  onPageChange(event: PageEvent): void {
    // MatPaginator is 0-indexed; API is 1-indexed
    this.loadPage(event.pageIndex + 1);
  }

  onSyncClick(): void {
    if (this.syncing()) {
      return;
    }

    this.syncing.set(true);

    this.discogsService.triggerSync().subscribe({
      next: (response) => {
        if (response.status === 202) {
          this.snackBar.open('Sync started. This may take a few minutes.', 'Dismiss', {
            duration: 5000,
          });
          setTimeout(() => this.loadPage(this.currentPage()), 10000);
        } else {
          this.syncing.set(false);
        }
      },
      error: (err: HttpErrorResponse) => {
        this.syncing.set(false);
        if (err.status === 409) {
          this.snackBar.open('A sync is already in progress.', 'Dismiss', { duration: 5000 });
        } else if (err.status === 503) {
          this.snackBar.open('Discogs token is not configured.', 'Dismiss', { duration: 5000 });
        } else {
          this.snackBar.open('An unexpected error occurred.', 'Dismiss', { duration: 5000 });
        }
      },
    });
  }

  private loadPage(page: number): void {
    this.loading.set(true);
    this.currentPage.set(page);

    this.discogsService.getCollection(page, this.pageSize).subscribe({
      next: (result) => {
        this.pagedResult.set(result);
        this.loading.set(false);
        this.syncing.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.syncing.set(false);
        this.snackBar.open('Failed to load collection.', 'Dismiss', { duration: 5000 });
      },
    });
  }
}
