import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { BookDto, HardcoverService } from '../hardcover.service';
import { SyncStateService } from '../../../core/sync/sync-state.service';

@Component({
  selector: 'app-books',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatTableModule,
    RouterModule,
  ],
  templateUrl: './books.component.html',
})
export class BooksComponent implements OnInit, OnDestroy {
  books = signal<BookDto[]>([]);
  currentPage = signal(1);
  readonly displayedColumns = ['thumbnail', 'author', 'title', 'genre', 'year'];
  private readonly hardcoverService = inject(HardcoverService);
  loading = signal(true);
  readonly pageSize = 25;
  private readonly snackBar = inject(MatSnackBar);
  private subscription?: Subscription;
  private readonly syncState = inject(SyncStateService);
  totalCount = signal(0);

  loadBooks(page: number): void {
    this.loading.set(true);
    this.hardcoverService.getBooks(page, this.pageSize).subscribe({
      next: (result) => {
        this.books.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load books.', 'Dismiss', { duration: 5000 });
      },
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  ngOnInit(): void {
    this.loadBooks(this.currentPage());
    this.subscription = this.syncState.booksSyncCompleted$.subscribe(() => {
      this.loadBooks(this.currentPage());
    });
  }

  onPageChange(event: PageEvent): void {
    this.currentPage.set(event.pageIndex + 1);
    this.loadBooks(this.currentPage());
  }
}
