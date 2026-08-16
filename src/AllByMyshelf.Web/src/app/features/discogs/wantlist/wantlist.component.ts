import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { WantlistService, WantlistReleaseDto } from '../../../core/discogs/wantlist.service';
import { FormatIconPipe } from '../format-icon.pipe';
import {
  applySortChange,
  readSortColumns,
  SortColumn,
  sortItems,
  writeSortColumns,
} from '../../../shared/table-sort';

@Component({
  selector: 'app-wantlist',
  standalone: true,
  imports: [
    CommonModule,
    FormatIconPipe,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatSortModule,
    MatTableModule,
    RouterModule,
  ],
  templateUrl: './wantlist.component.html',
  styleUrl: './wantlist.component.scss',
})
export class WantlistComponent implements OnInit {
  private static readonly sortStorageKey = 'wantlist-sort-columns';

  currentPage = signal(1);
  readonly displayedColumns = ['thumbnail', 'artist', 'title', 'year', 'format', 'genre'];
  loading = signal(true);
  readonly pageSize = 25;
  releases = signal<WantlistReleaseDto[]>([]);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  sortActive = signal('artist');
  sortColumns = signal<SortColumn[]>([]);
  sortDirection = signal<'asc' | 'desc'>('asc');
  totalCount = signal(0);
  private readonly wantlistService = inject(WantlistService);

  protected columnValue(r: WantlistReleaseDto, col: string): string {
    switch (col) {
      case 'artist':
        return r.artists.join(', ');
      case 'format':
        return r.format;
      case 'genre':
        return r.genre ?? '—';
      case 'title':
        return r.title;
      case 'year':
        return r.year?.toString() ?? '—';
      default:
        return '';
    }
  }

  protected expandArtists(artists: string[]): string[] {
    return artists
      .flatMap((a) => a.split(','))
      .map((a) => a.replace(/\s*\(\d+\)$/, '').trim())
      .filter((a) => a.length > 0);
  }

  /**
   * The whole wantlist is fetched once so sorting and paging apply across every
   * entry rather than only the page currently on screen.
   */
  private loadWantlist(): void {
    this.loading.set(true);
    this.wantlistService.getWantlist(1, 10000).subscribe({
      next: (result) => {
        this.releases.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load wantlist.', 'Dismiss', { duration: 5000 });
      },
    });
  }

  ngOnInit(): void {
    const columns = readSortColumns(WantlistComponent.sortStorageKey, [
      { active: 'artist', direction: 'asc' },
      { active: 'title', direction: 'asc' },
    ]);

    this.sortColumns.set(columns);
    this.sortActive.set(columns[0].active);
    this.sortDirection.set(columns[0].direction);

    this.loadWantlist();
  }

  onPageChange(event: PageEvent): void {
    this.currentPage.set(event.pageIndex + 1);
  }

  onRowClick(release: WantlistReleaseDto): void {
    this.router.navigate(['/releases', release.id]);
  }

  onSortChange(sort: Sort): void {
    const columns = applySortChange(this.sortColumns(), sort);

    this.sortActive.set(sort.active);
    this.sortColumns.set(columns);
    this.sortDirection.set(columns[0].direction);
    this.currentPage.set(1);

    writeSortColumns(WantlistComponent.sortStorageKey, columns);
  }

  get pagedReleases(): WantlistReleaseDto[] {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.sortedReleases.slice(start, start + this.pageSize);
  }

  get sortedReleases(): WantlistReleaseDto[] {
    return sortItems(this.releases(), this.sortColumns(), (item, col) =>
      this.columnValue(item, col),
    );
  }
}
