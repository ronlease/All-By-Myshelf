import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { Observable } from 'rxjs';
import { BoardGameGeekService, BoardGameDto } from '../board-game-geek.service';
import { CollectionBaseComponent } from '../../../shared/collection-base.component';

@Component({
  selector: 'app-board-games',
  standalone: true,
  imports: [
    CommonModule,
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
    MatTableModule,
    RouterModule,
  ],
  templateUrl: './board-games.component.html',
})
export class BoardGamesComponent extends CollectionBaseComponent<BoardGameDto> {
  protected allItems = signal<BoardGameDto[]>([]);
  private readonly boardGameGeekService = inject(BoardGameGeekService);
  protected readonly collectionKey = 'board-games';
  protected readonly displayedColumns = ['thumbnail', 'title', 'designer', 'genre', 'players', 'yearPublished'];
  protected readonly groupByOptions = [
    { label: 'No grouping', value: '' },
    { label: 'Designer', value: 'designer' },
    { label: 'Decade', value: 'decade' },
    { label: 'Genre', value: 'genre' },
    { label: 'Year', value: 'year' },
  ];
  protected readonly pageSize = 25;

  // Alias for template compatibility
  get allGames() {
    return this.allItems;
  }

  protected applySearch(games: BoardGameDto[]): BoardGameDto[] {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) return games;

    return games.filter(g =>
      g.title.toLowerCase().includes(term) ||
      g.designers.some(d => d.toLowerCase().includes(term)) ||
      (g.genre ?? '').toLowerCase().includes(term) ||
      (g.yearPublished?.toString() ?? '').includes(term)
    );
  }

  protected columnValue(g: BoardGameDto, col: string): string {
    switch (col) {
      case 'designer': return g.designers.length > 0 ? g.designers.join(', ') : '—';
      case 'genre': return g.genre ?? '—';
      case 'title': return g.title;
      case 'year': return g.yearPublished?.toString() ?? '—';
      default: return '';
    }
  }

  protected expandDesigners(designers: string[]): string[] {
    return designers
      .flatMap(d => d.split(','))
      .map(d => d.trim())
      .filter(d => d.length > 0);
  }

  protected detailRoute(game: BoardGameDto): string {
    return `/board-games/${game.id}`;
  }

  // Alias for template compatibility
  get filteredGames(): BoardGameDto[] {
    return this.filteredItems;
  }

  formatPlayers(game: BoardGameDto): string {
    if (game.minPlayers === null && game.maxPlayers === null) return '—';
    if (game.minPlayers === game.maxPlayers) return game.minPlayers?.toString() ?? '—';
    return `${game.minPlayers ?? '?'}–${game.maxPlayers ?? '?'}`;
  }

  protected getDecadeKey(game: BoardGameDto): string {
    return game.yearPublished != null ? `${Math.floor(game.yearPublished / 10) * 10}s` : '—';
  }

  // Alias for template compatibility
  get groupedGames(): { items: BoardGameDto[]; key: string }[] {
    return this.groupedItems;
  }

  protected loadAll(): void {
    this.loading.set(true);
    this.boardGameGeekService.getBoardGames(1, 10000).subscribe({
      next: (result) => {
        this.allItems.set(result.items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load board games.', 'Dismiss', { duration: 5000 });
      },
    });
  }

  // Alias for template compatibility
  get pagedGames(): BoardGameDto[] {
    return this.pagedItems;
  }

  protected syncCompletedObservable(): Observable<void> {
    return this.syncState.gamesSyncCompleted$;
  }
}
