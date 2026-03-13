import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { Subscription } from 'rxjs';
import { DiscogsService, ReleaseDto } from '../discogs.service';
import { FormatIconPipe } from '../format-icon.pipe';
import { SyncStateService } from '../../../core/sync/sync-state.service';

@Component({
  selector: 'app-collection',
  standalone: true,
  imports: [
    CommonModule,
    FormatIconPipe,
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
  templateUrl: './collection.component.html',
})
export class CollectionComponent implements OnInit, OnDestroy {
  allReleases = signal<ReleaseDto[]>([]);
  artistFilter = signal<string[]>([]);
  currentPage = signal(1);
  private readonly discogsService = inject(DiscogsService);
  readonly displayedColumns = ['thumbnail', 'artist', 'title', 'genre', 'year', 'format'];
  expandedGroups = signal<Set<string>>(new Set());
  formatFilter = signal<string[]>([]);
  genreFilter = signal<string[]>([]);
  readonly groupByOptions = [
    { label: 'No grouping', value: '' },
    { label: 'Artist', value: 'artist' },
    { label: 'Decade', value: 'decade' },
    { label: 'Format', value: 'format' },
    { label: 'Genre', value: 'genre' },
    { label: 'Year', value: 'year' },
  ];
  groupByField = signal('');
  loading = signal(true);
  readonly pageSize = 20;
  recentReleases = signal<ReleaseDto[]>([]);
  private readonly router = inject(Router);
  private searchTimer?: ReturnType<typeof setTimeout>;
  searchTerm = signal('');
  private readonly snackBar = inject(MatSnackBar);
  private subscription?: Subscription;
  private readonly syncState = inject(SyncStateService);
  yearFilter = signal<string[]>([]);

  // ── Computed ────────────────────────────────────────────────────────────────

  get filteredReleases(): ReleaseDto[] {
    let releases = this.allReleases();

    const term = this.searchTerm().toLowerCase().trim();
    if (term) {
      releases = releases.filter(r =>
        r.artist.toLowerCase().includes(term) ||
        r.title.toLowerCase().includes(term) ||
        r.format.toLowerCase().includes(term) ||
        (r.genre ?? '').toLowerCase().includes(term) ||
        (r.year?.toString() ?? '').includes(term)
      );
    }

    const af = this.artistFilter();
    if (af.length) releases = releases.filter(r => af.includes(this.columnValue(r, 'artist')));

    const ff = this.formatFilter();
    if (ff.length) releases = releases.filter(r => ff.includes(this.columnValue(r, 'format')));

    const gf = this.genreFilter();
    if (gf.length) releases = releases.filter(r => gf.includes(this.columnValue(r, 'genre')));

    const yf = this.yearFilter();
    if (yf.length) releases = releases.filter(r => yf.includes(this.columnValue(r, 'year')));

    return releases;
  }

  get groupedReleases(): { items: ReleaseDto[]; key: string }[] {
    const field = this.groupByField();
    if (!field) return [];

    const map = new Map<string, ReleaseDto[]>();
    for (const r of this.filteredReleases) {
      let key: string;
      if (field === 'decade') {
        key = r.year != null ? `${Math.floor(r.year / 10) * 10}s` : '—';
      } else {
        key = this.columnValue(r, field);
      }
      const group = map.get(key) ?? [];
      group.push(r);
      map.set(key, group);
    }

    return Array.from(map.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([key, items]) => ({ items, key }));
  }

  get pagedReleases(): ReleaseDto[] {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredReleases.slice(start, start + this.pageSize);
  }

  get totalFilteredCount(): number {
    return this.filteredReleases.length;
  }

  // ── Helpers ─────────────────────────────────────────────────────────────────

  private columnValue(r: ReleaseDto, col: string): string {
    switch (col) {
      case 'artist': return r.artist;
      case 'format': return r.format;
      case 'genre': return r.genre ?? '—';
      case 'title': return r.title;
      case 'year': return r.year?.toString() ?? '—';
      default: return '';
    }
  }

  distinctValues(col: string): string[] {
    const seen = new Set<string>();
    for (const r of this.allReleases()) {
      seen.add(this.columnValue(r, col));
    }
    return Array.from(seen).sort((a, b) => a.localeCompare(b));
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

  isGroupExpanded(key: string): boolean {
    return this.expandedGroups().has(key);
  }

  // ── Data loading ─────────────────────────────────────────────────────────────

  private loadAll(): void {
    this.loading.set(true);
    this.discogsService.getCollection(1, 10000).subscribe({
      next: (result) => {
        this.allReleases.set(result.items);
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

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  ngOnInit(): void {
    this.loadAll();
    this.loadRecentlyAdded();
    this.subscription = this.syncState.discogsSyncCompleted$.subscribe(() => {
      this.loadAll();
      this.loadRecentlyAdded();
    });
  }

  // ── Event handlers ───────────────────────────────────────────────────────────

  onColumnFilterChange(col: string, values: string[]): void {
    switch (col) {
      case 'artist': this.artistFilter.set(values); break;
      case 'format': this.formatFilter.set(values); break;
      case 'genre': this.genreFilter.set(values); break;
      case 'year': this.yearFilter.set(values); break;
    }
    this.currentPage.set(1);
  }

  onGroupByChange(): void {
    this.expandedGroups.set(new Set());
  }

  onGroupToggle(key: string): void {
    const current = new Set(this.expandedGroups());
    if (current.has(key)) {
      current.delete(key);
    } else {
      current.add(key);
    }
    this.expandedGroups.set(current);
  }

  onPageChange(event: PageEvent): void {
    this.currentPage.set(event.pageIndex + 1);
  }

  onRowClick(release: ReleaseDto): void {
    this.router.navigate(['/releases', release.id]);
  }

  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.currentPage.set(1), 300);
  }
}
