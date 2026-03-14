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
import { BggService, BoardGameDto } from '../bgg.service';
import { SyncStateService } from '../../../core/sync/sync-state.service';

@Component({
  selector: 'app-board-games',
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
  templateUrl: './board-games.component.html',
})
export class BoardGamesComponent implements OnInit, OnDestroy {
  allGames = signal<BoardGameDto[]>([]);
  currentPage = signal(1);
  readonly displayedColumns = ['thumbnail', 'title', 'designer', 'genre', 'players', 'yearPublished'];
  expandedGroups = signal<Set<string>>(new Set());
  readonly groupByOptions = [
    { label: 'No grouping', value: '' },
    { label: 'Designer', value: 'designer' },
    { label: 'Decade', value: 'decade' },
    { label: 'Genre', value: 'genre' },
    { label: 'Year', value: 'year' },
  ];
  groupByField = signal('');
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly bggService = inject(BggService);
  loading = signal(true);
  readonly pageSize = 25;
  private readonly router = inject(Router);
  private searchTimer?: ReturnType<typeof setTimeout>;
  searchTerm = signal('');
  private readonly snackBar = inject(MatSnackBar);
  private subscription?: Subscription;
  private readonly syncState = inject(SyncStateService);

  // ── Computed ────────────────────────────────────────────────────────────────

  get filteredGames(): BoardGameDto[] {
    let games = this.allGames();

    const term = this.searchTerm().toLowerCase().trim();
    if (term) {
      games = games.filter(g =>
        g.title.toLowerCase().includes(term) ||
        (g.designer ?? '').toLowerCase().includes(term) ||
        (g.genre ?? '').toLowerCase().includes(term) ||
        (g.yearPublished?.toString() ?? '').includes(term)
      );
    }

    return games;
  }

  get groupedGames(): { items: BoardGameDto[]; key: string }[] {
    const field = this.groupByField();
    if (!field) return [];

    const map = new Map<string, BoardGameDto[]>();
    for (const g of this.filteredGames) {
      let key: string;
      if (field === 'decade') {
        key = g.yearPublished != null ? `${Math.floor(g.yearPublished / 10) * 10}s` : '—';
      } else {
        key = this.columnValue(g, field);
      }
      const group = map.get(key) ?? [];
      group.push(g);
      map.set(key, group);
    }

    return Array.from(map.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([key, items]) => ({ items, key }));
  }

  get pagedGames(): BoardGameDto[] {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredGames.slice(start, start + this.pageSize);
  }

  get totalFilteredCount(): number {
    return this.filteredGames.length;
  }

  // ── Helpers ─────────────────────────────────────────────────────────────────

  private columnValue(g: BoardGameDto, col: string): string {
    switch (col) {
      case 'designer': return g.designer ?? '—';
      case 'genre': return g.genre ?? '—';
      case 'title': return g.title;
      case 'year': return g.yearPublished?.toString() ?? '—';
      default: return '';
    }
  }

  formatPlayers(game: BoardGameDto): string {
    if (game.minPlayers === null && game.maxPlayers === null) return '—';
    if (game.minPlayers === game.maxPlayers) return game.minPlayers?.toString() ?? '—';
    return `${game.minPlayers ?? '?'}–${game.maxPlayers ?? '?'}`;
  }

  isGroupExpanded(key: string): boolean {
    return this.expandedGroups().has(key);
  }

  // ── Data loading ─────────────────────────────────────────────────────────────

  private loadAll(): void {
    this.loading.set(true);
    this.bggService.getBoardGames(1, 10000).subscribe({
      next: (result) => {
        this.allGames.set(result.items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load board games.', 'Dismiss', { duration: 5000 });
      },
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  ngOnInit(): void {
    localStorage.setItem('last-collection', 'board-games');

    const params = this.activatedRoute.snapshot.queryParams;
    if (params['groupBy']) {
      this.groupByField.set(params['groupBy']);
    }
    if (params['expand']) {
      this.expandedGroups.set(new Set([params['expand']]));
    }

    this.loadAll();
    this.subscription = this.syncState.gamesSyncCompleted$.subscribe(() => {
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

  onRowClick(game: BoardGameDto): void {
    this.router.navigate(['/board-games', game.id]);
  }

  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.currentPage.set(1), 300);
  }
}
