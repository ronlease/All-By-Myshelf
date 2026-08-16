import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { DiscogsService, MaintenanceReleaseDto } from '../discogs.service';
import {
  applySortChange,
  readSortColumns,
  SortColumn,
  sortItems,
  writeSortColumns,
} from '../../../shared/table-sort';

@Component({
  selector: 'app-maintenance',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatSortModule,
    MatTableModule,
  ],
  templateUrl: './maintenance.component.html',
  styleUrl: './maintenance.component.scss',
})
export class MaintenanceComponent implements OnInit {
  private static readonly sortStorageKey = 'maintenance-sort-columns';

  readonly displayedColumns = ['thumbnail', 'artist', 'title', 'missingFields', 'discogs'];
  private readonly discogsService = inject(DiscogsService);
  loading = signal(true);
  releases = signal<MaintenanceReleaseDto[]>([]);
  private readonly snackBar = inject(MatSnackBar);
  sortActive = signal('artist');
  sortColumns = signal<SortColumn[]>([]);
  sortDirection = signal<'asc' | 'desc'>('asc');

  protected columnValue(r: MaintenanceReleaseDto, col: string): string {
    switch (col) {
      case 'artist':
        return r.artists.join(', ');
      case 'missingFields':
        return r.missingFields.join(', ');
      case 'title':
        return r.title;
      default:
        return '';
    }
  }

  ngOnInit(): void {
    const columns = readSortColumns(MaintenanceComponent.sortStorageKey, [
      { active: 'artist', direction: 'asc' },
      { active: 'title', direction: 'asc' },
    ]);

    this.sortColumns.set(columns);
    this.sortActive.set(columns[0].active);
    this.sortDirection.set(columns[0].direction);

    this.discogsService.getIncompleteReleases().subscribe({
      next: (releases) => {
        this.releases.set(releases);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load maintenance data.', 'Dismiss', { duration: 5000 });
      },
    });
  }

  onSortChange(sort: Sort): void {
    const columns = applySortChange(this.sortColumns(), sort);

    this.sortActive.set(sort.active);
    this.sortColumns.set(columns);
    this.sortDirection.set(columns[0].direction);

    writeSortColumns(MaintenanceComponent.sortStorageKey, columns);
  }

  get sortedReleases(): MaintenanceReleaseDto[] {
    return sortItems(this.releases(), this.sortColumns(), (item, col) =>
      this.columnValue(item, col),
    );
  }
}
