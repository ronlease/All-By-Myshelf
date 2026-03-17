import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
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
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
    MatTableModule,
    RouterModule,
  ],
  templateUrl: './books.component.html',
})
export class BooksComponent implements OnInit, OnDestroy {
  allBooks = signal<BookDto[]>([]);
  currentPage = signal(1);
  readonly displayedColumns = ['thumbnail', 'author', 'title', 'genre', 'year'];
  expandedGroups = signal<Set<string>>(new Set());
  readonly groupByOptions = [
    { label: 'No grouping', value: '' },
    { label: 'Author', value: 'author' },
    { label: 'Decade', value: 'decade' },
    { label: 'Genre', value: 'genre' },
    { label: 'Year', value: 'year' },
  ];
  groupByField = signal('');
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly hardcoverService = inject(HardcoverService);
  loading = signal(true);
  readonly pageSize = 25;
  private readonly router = inject(Router);
  private searchTimer?: ReturnType<typeof setTimeout>;
  searchTerm = signal('');
  private readonly snackBar = inject(MatSnackBar);
  private subscription?: Subscription;
  private readonly syncState = inject(SyncStateService);

  // ── Computed ────────────────────────────────────────────────────────────────

  get filteredBooks(): BookDto[] {
    let books = this.allBooks();

    const term = this.searchTerm().toLowerCase().trim();
    if (term) {
      books = books.filter(b =>
        b.title.toLowerCase().includes(term) ||
        b.authors.some(a => a.toLowerCase().includes(term)) ||
        (b.genre ?? '').toLowerCase().includes(term) ||
        (b.year?.toString() ?? '').includes(term)
      );
    }

    return books;
  }

  get groupedBooks(): { items: BookDto[]; key: string }[] {
    const field = this.groupByField();
    if (!field) return [];

    const map = new Map<string, BookDto[]>();
    for (const b of this.filteredBooks) {
      let key: string;
      if (field === 'decade') {
        key = b.year != null ? `${Math.floor(b.year / 10) * 10}s` : '—';
      } else {
        key = this.columnValue(b, field);
      }
      const group = map.get(key) ?? [];
      group.push(b);
      map.set(key, group);
    }

    return Array.from(map.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([key, items]) => ({ items, key }));
  }

  get pagedBooks(): BookDto[] {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredBooks.slice(start, start + this.pageSize);
  }

  get totalFilteredCount(): number {
    return this.filteredBooks.length;
  }

  // ── Helpers ─────────────────────────────────────────────────────────────────

  private columnValue(b: BookDto, col: string): string {
    switch (col) {
      case 'author': return b.authors.length > 0 ? b.authors.join(', ') : '—';
      case 'genre': return b.genre ?? '—';
      case 'title': return b.title;
      case 'year': return b.year?.toString() ?? '—';
      default: return '';
    }
  }

  isGroupExpanded(key: string): boolean {
    return this.expandedGroups().has(key);
  }

  // ── Data loading ─────────────────────────────────────────────────────────────

  private loadAll(): void {
    this.loading.set(true);
    this.hardcoverService.getBooks(1, 10000).subscribe({
      next: (result) => {
        this.allBooks.set(result.items);
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
    localStorage.setItem('last-collection', 'books');

    const params = this.activatedRoute.snapshot.queryParams;
    if (params['groupBy']) {
      this.groupByField.set(params['groupBy']);
    }
    if (params['expand']) {
      this.expandedGroups.set(new Set([params['expand']]));
    }

    this.loadAll();
    this.subscription = this.syncState.booksSyncCompleted$.subscribe(() => {
      this.loadAll();
    });
  }

  // ── Event handlers ───────────────────────────────────────────────────────────

  onGroupByChange(): void {
    this.expandedGroups.set(new Set());
  }

  onGroupCollapse(key: string): void {
    const current = new Set(this.expandedGroups());
    current.delete(key);
    this.expandedGroups.set(current);
  }

  onGroupExpand(key: string): void {
    const current = new Set(this.expandedGroups());
    current.add(key);
    this.expandedGroups.set(current);
  }

  onPageChange(event: PageEvent): void {
    this.currentPage.set(event.pageIndex + 1);
  }

  onRowClick(book: BookDto): void {
    this.router.navigate(['/books', book.id]);
  }

  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.currentPage.set(1), 300);
  }
}
