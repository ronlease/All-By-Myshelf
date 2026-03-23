import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { Observable } from 'rxjs';
import { DiscogsService, ReleaseDto } from '../discogs.service';
import { FormatIconPipe } from '../format-icon.pipe';
import { CollectionBaseComponent } from '../../../shared/collection-base.component';

interface SortColumn {
  active: string;
  direction: 'asc' | 'desc';
}

@Component({
  selector: 'app-collection',
  standalone: true,
  imports: [
    CommonModule,
    FormatIconPipe,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
    MatSortModule,
    MatTableModule,
    RouterModule,
  ],
  templateUrl: './collection.component.html',
})
export class CollectionComponent extends CollectionBaseComponent<ReleaseDto> {
  protected allItems = signal<ReleaseDto[]>([]);
  artistFilter = signal<string[]>([]);
  protected readonly collectionKey = 'records';
  private readonly discogsService = inject(DiscogsService);
  protected readonly displayedColumns = ['thumbnail', 'artist', 'title', 'genre', 'year', 'format'];
  formatFilter = signal<string[]>([]);
  genreFilter = signal<string[]>([]);
  protected readonly groupByOptions = [
    { label: 'No grouping', value: '' },
    { label: 'Artist', value: 'artist' },
    { label: 'Decade', value: 'decade' },
    { label: 'Format', value: 'format' },
    { label: 'Genre', value: 'genre' },
    { label: 'Year', value: 'year' },
  ];
  protected readonly pageSize = 20; // Base class requirement; actual size is in currentPageSize
  currentPageSize = signal(parseInt(localStorage.getItem('music-page-size') ?? '20', 10));
  pageSizeOptions = [10, 20, 50, 100];
  recentReleases = signal<ReleaseDto[]>([]);
  sortActive = signal('artist');
  sortColumns = signal<SortColumn[]>(this.loadSortColumns());
  sortDirection = signal<'asc' | 'desc'>('asc');
  yearFilter = signal<string[]>([]);

  // Alias for template compatibility
  get allReleases() {
    return this.allItems;
  }

  protected applySearch(releases: ReleaseDto[]): ReleaseDto[] {
    const term = this.searchTerm().toLowerCase().trim();
    if (term) {
      releases = releases.filter(r =>
        r.artists.some(a => a.toLowerCase().includes(term)) ||
        (r.trackArtists ?? []).some(a => a.toLowerCase().includes(term)) ||
        r.title.toLowerCase().includes(term) ||
        r.format.toLowerCase().includes(term) ||
        (r.genre ?? '').toLowerCase().includes(term) ||
        (r.year?.toString() ?? '').includes(term)
      );
    }

    // Apply column filters (Discogs-specific)
    const af = this.artistFilter();
    if (af.length) releases = releases.filter(r => af.includes(this.columnValue(r, 'artist')));

    const ff = this.formatFilter();
    if (ff.length) releases = releases.filter(r => ff.includes(this.columnValue(r, 'format')));

    const gf = this.genreFilter();
    if (gf.length) releases = releases.filter(r => gf.includes(this.columnValue(r, 'genre')));

    const yf = this.yearFilter();
    if (yf.length) releases = releases.filter(r => yf.includes(this.columnValue(r, 'year')));

    // Apply multi-column sorting
    const cols = this.sortColumns();
    releases = [...releases].sort((a, b) => {
      for (const col of cols) {
        const aVal = this.columnValue(a, col.active);
        const bVal = this.columnValue(b, col.active);
        const comparison = aVal.localeCompare(bVal);
        if (comparison !== 0) {
          return col.direction === 'asc' ? comparison : -comparison;
        }
      }
      return 0;
    });

    return releases;
  }

  protected columnValue(r: ReleaseDto, col: string): string {
    switch (col) {
      case 'artist': return r.artists.join(', ');
      case 'format': return r.format;
      case 'genre': return r.genre ?? '—';
      case 'title': return r.title;
      case 'year': return r.year?.toString() ?? '—';
      default: return '';
    }
  }

  protected detailRoute(release: ReleaseDto): string {
    return `/releases/${release.id}`;
  }

  distinctValues(col: string): string[] {
    const seen = new Set<string>();
    for (const r of this.allItems()) {
      seen.add(this.columnValue(r, col));
    }
    return Array.from(seen).sort((a, b) => a.localeCompare(b));
  }

  protected expandArtists(artists: string[]): string[] {
    return artists
      .flatMap(a => a.split(','))
      .map(a => a.replace(/\s*\(\d+\)$/, '').trim())
      .filter(a => a.length > 0);
  }

  // Alias for template compatibility
  get filteredReleases(): ReleaseDto[] {
    return this.filteredItems;
  }

  protected getDecadeKey(release: ReleaseDto): string {
    return release.year != null ? `${Math.floor(release.year / 10) * 10}s` : '—';
  }

  // Alias for template compatibility
  get groupedReleases(): { items: ReleaseDto[]; key: string }[] {
    return this.groupedItems;
  }

  hasActiveFilter(col: string): boolean {
    switch (col) {
      case 'artist': return this.artistFilter().length > 0;
      case 'format': return this.formatFilter().length > 0;
      case 'genre': return this.genreFilter().length > 0;
      case 'year': return this.yearFilter().length > 0;
      default: return false;
    }
  }

  protected loadAll(): void {
    this.loading.set(true);
    this.discogsService.getCollection(1, 10000).subscribe({
      next: (result) => {
        this.allItems.set(result.items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load collection.', 'Dismiss', { duration: 5000 });
      },
    });
  }

  private loadRecentlyAdded(): void {
    this.discogsService.getRecentlyAdded().subscribe({
      next: (releases) => this.recentReleases.set(releases),
    });
  }

  private loadSortColumns(): SortColumn[] {
    const saved = localStorage.getItem('music-sort-columns');
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch {
        // Fall through to default
      }
    }
    return [
      { active: 'artist', direction: 'asc' },
      { active: 'title', direction: 'asc' },
    ];
  }

  override ngOnInit(): void {
    localStorage.setItem('last-collection', this.collectionKey);

    const params = this.activatedRoute.snapshot.queryParams;
    if (params['groupBy']) {
      this.groupByField.set(params['groupBy']);
    }
    if (params['expand']) {
      this.expandedGroups.set(new Set([params['expand']]));
    }

    // Restore persisted sort state to mat-sort header
    const cols = this.sortColumns();
    if (cols.length > 0) {
      this.sortActive.set(cols[0].active);
      this.sortDirection.set(cols[0].direction);
    }

    this.loadAll();
    this.loadRecentlyAdded(); // Discogs-specific
    this.subscription = this.syncCompletedObservable().subscribe(() => {
      this.loadAll();
      this.loadRecentlyAdded(); // Discogs-specific: reload recently added on sync
    });
  }

  onColumnFilterChange(col: string, values: string[]): void {
    switch (col) {
      case 'artist': this.artistFilter.set(values); break;
      case 'format': this.formatFilter.set(values); break;
      case 'genre': this.genreFilter.set(values); break;
      case 'year': this.yearFilter.set(values); break;
    }
    this.currentPage.set(1);
  }

  override onPageChange(event: PageEvent): void {
    this.currentPage.set(event.pageIndex + 1);
    if (event.pageSize !== this.currentPageSize()) {
      this.currentPageSize.set(event.pageSize);
      localStorage.setItem('music-page-size', event.pageSize.toString());
      this.currentPage.set(1);
    }
  }

  onSortChange(sort: Sort): void {
    this.sortActive.set(sort.active);
    this.sortDirection.set(sort.direction as 'asc' | 'desc' || 'asc');

    const dir = (sort.direction as 'asc' | 'desc') || 'asc';
    const cols = this.sortColumns().filter(c => c.active !== sort.active);
    cols.unshift({ active: sort.active, direction: dir });
    this.sortColumns.set(cols);
    localStorage.setItem('music-sort-columns', JSON.stringify(cols));
  }

  // Alias for template compatibility
  get pagedReleases(): ReleaseDto[] {
    const size = this.currentPageSize();
    const start = (this.currentPage() - 1) * size;
    return this.filteredItems.slice(start, start + size);
  }

  protected syncCompletedObservable(): Observable<void> {
    return this.syncState.discogsSyncCompleted$;
  }
}
