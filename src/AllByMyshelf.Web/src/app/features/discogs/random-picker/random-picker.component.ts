import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatToolbarModule } from '@angular/material/toolbar';

import { DiscogsService, ReleaseDetailDto, ReleaseDto } from '../discogs.service';
import { BoardGameDto, BggService } from '../../bgg/bgg.service';
import { BookDto, HardcoverService } from '../../hardcover/hardcover.service';
import { FeaturesService } from '../../../core/config/features.service';
import { FormatIconPipe } from '../format-icon.pipe';

type PickContext = 'records' | 'books' | 'board-games';
type PickResult = ReleaseDetailDto | BookDto | BoardGameDto;

@Component({
  selector: 'app-random-picker',
  standalone: true,
  imports: [
    FormatIconPipe,
    FormsModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatListModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatToolbarModule,
    RouterModule,
  ],
  templateUrl: './random-picker.component.html',
})
export class RandomPickerComponent implements OnInit {
  allBoardGames = signal<BoardGameDto[]>([]);
  allBooks = signal<BookDto[]>([]);
  allReleases = signal<ReleaseDto[]>([]);
  bggEnabled = signal(false);
  private readonly bggService = inject(BggService);
  bookAuthorFilter = signal('');
  bookGenreFilter = signal('');
  context = signal<PickContext>('records');
  decadeFilter = signal('');
  private readonly discogsService = inject(DiscogsService);
  discogsEnabled = signal(false);
  private readonly featuresService = inject(FeaturesService);
  formatFilter = signal('');
  genreFilter = signal('');
  hardcoverEnabled = signal(false);
  private readonly hardcoverService = inject(HardcoverService);
  loading = signal(true);
  maxResults = signal(1);
  readonly maxResultsOptions = [1, 3, 5, 10];
  picking = signal(false);
  results = signal<PickResult[]>([]);
  private readonly router = inject(Router);

  get distinctBookAuthors(): string[] {
    const seen = new Set<string>();
    for (const b of this.allBooks()) {
      if (b.author) seen.add(b.author);
    }
    return Array.from(seen).sort((a, b) => a.localeCompare(b));
  }

  get distinctBookGenres(): string[] {
    const seen = new Set<string>();
    for (const b of this.allBooks()) {
      if (b.genre) seen.add(b.genre);
    }
    return Array.from(seen).sort((a, b) => a.localeCompare(b));
  }

  get distinctDecades(): string[] {
    const seen = new Set<string>();
    for (const r of this.allReleases()) {
      if (r.year != null) seen.add(`${Math.floor(r.year / 10) * 10}s`);
    }
    return Array.from(seen).sort((a, b) => a.localeCompare(b));
  }

  get distinctFormats(): string[] {
    const seen = new Set<string>();
    for (const r of this.allReleases()) seen.add(r.format);
    return Array.from(seen).sort((a, b) => a.localeCompare(b));
  }

  get distinctGenres(): string[] {
    const seen = new Set<string>();
    for (const r of this.allReleases()) {
      if (r.genre) seen.add(r.genre);
    }
    return Array.from(seen).sort((a, b) => a.localeCompare(b));
  }

  isBoardGame(result: PickResult): result is BoardGameDto {
    return result !== null && 'bggId' in result;
  }

  isBook(result: PickResult): result is BookDto {
    return result !== null && 'hardcoverId' in result;
  }

  isRelease(result: PickResult): result is ReleaseDetailDto {
    return result !== null && 'discogsId' in result;
  }

  ngOnInit(): void {
    this.featuresService.getFeatures().subscribe(features => {
      this.bggEnabled.set(features.bggEnabled);
      this.discogsEnabled.set(features.discogsEnabled);
      this.hardcoverEnabled.set(features.hardcoverEnabled);

      const lastCollection = localStorage.getItem('last-collection');
      if (lastCollection === 'board-games' && features.bggEnabled) {
        this.context.set('board-games');
      } else if (lastCollection === 'books' && features.hardcoverEnabled) {
        this.context.set('books');
      } else if (features.discogsEnabled) {
        this.context.set('records');
      } else if (features.hardcoverEnabled) {
        this.context.set('books');
      } else if (features.bggEnabled) {
        this.context.set('board-games');
      }
    });

    const savedMaxResults = localStorage.getItem('random-picker-max-results');
    if (savedMaxResults) {
      const parsed = parseInt(savedMaxResults, 10);
      if (this.maxResultsOptions.includes(parsed)) {
        this.maxResults.set(parsed);
      }
    }

    this.discogsService.getCollection(1, 10000).subscribe({
      next: (res) => {
        this.allReleases.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });

    this.hardcoverService.getBooks(1, 10000).subscribe({
      next: (res) => {
        this.allBooks.set(res.items);
      },
      error: () => this.loading.set(false),
    });

    this.bggService.getBoardGames(1, 10000).subscribe({
      next: (res) => {
        this.allBoardGames.set(res.items);
      },
      error: () => this.loading.set(false),
    });
  }

  onBackClick(): void {
    this.router.navigate(['/']);
  }

  onContextChange(): void {
    this.results.set([]);
  }

  onMaxResultsChange(): void {
    localStorage.setItem('random-picker-max-results', this.maxResults().toString());
  }

  onPickClick(): void {
    this.picking.set(true);

    if (this.context() === 'records') {
      this.discogsService.getRandomRelease({
        decade: this.decadeFilter() || undefined,
        format: this.formatFilter() || undefined,
        genre: this.genreFilter() || undefined,
      }).subscribe({
        next: (release) => {
          const current = this.results();
          const updated = [release, ...current].slice(0, this.maxResults());
          this.results.set(updated);
          this.picking.set(false);
        },
        error: () => this.picking.set(false),
      });
    } else if (this.context() === 'books') {
      let filtered = this.allBooks();
      const authorFilter = this.bookAuthorFilter();
      const genreFilter = this.bookGenreFilter();

      if (authorFilter) {
        filtered = filtered.filter(b => b.author === authorFilter);
      }
      if (genreFilter) {
        filtered = filtered.filter(b => b.genre === genreFilter);
      }

      if (filtered.length > 0) {
        const randomIndex = Math.floor(Math.random() * filtered.length);
        const picked = filtered[randomIndex];
        const current = this.results();
        const updated = [picked, ...current].slice(0, this.maxResults());
        this.results.set(updated);
      }
      this.picking.set(false);
    } else if (this.context() === 'board-games') {
      const games = this.allBoardGames();
      if (games.length > 0) {
        const randomIndex = Math.floor(Math.random() * games.length);
        const picked = games[randomIndex];
        const current = this.results();
        const updated = [picked, ...current].slice(0, this.maxResults());
        this.results.set(updated);
      }
      this.picking.set(false);
    }
  }

  onViewDetailsClick(item: PickResult): void {
    if (this.isRelease(item)) {
      this.router.navigate(['/releases', item.id]);
    } else if (this.isBook(item)) {
      this.router.navigate(['/books', item.id]);
    } else if (this.isBoardGame(item)) {
      this.router.navigate(['/board-games', item.id]);
    }
  }
}
