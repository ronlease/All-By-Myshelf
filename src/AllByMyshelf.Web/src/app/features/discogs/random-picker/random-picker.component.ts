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
import { BookDto, HardcoverService } from '../../hardcover/hardcover.service';
import { FeaturesService } from '../../../core/config/features.service';
import { FormatIconPipe } from '../format-icon.pipe';

type PickContext = 'records' | 'books';
type PickResult = ReleaseDetailDto | BookDto | null;

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
  allBooks = signal<BookDto[]>([]);
  allReleases = signal<ReleaseDto[]>([]);
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
  picking = signal(false);
  result = signal<PickResult>(null);
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

  isBook(result: PickResult): result is BookDto {
    return result !== null && 'hardcoverId' in result;
  }

  isRelease(result: PickResult): result is ReleaseDetailDto {
    return result !== null && 'discogsId' in result;
  }

  ngOnInit(): void {
    this.featuresService.getFeatures().subscribe(features => {
      this.discogsEnabled.set(features.discogsEnabled);
      this.hardcoverEnabled.set(features.hardcoverEnabled);

      const lastCollection = localStorage.getItem('last-collection');
      if (lastCollection === 'books' && features.hardcoverEnabled) {
        this.context.set('books');
      } else if (features.discogsEnabled) {
        this.context.set('records');
      } else if (features.hardcoverEnabled) {
        this.context.set('books');
      }
    });

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
      error: () => {},
    });
  }

  onBackClick(): void {
    this.router.navigate(['/']);
  }

  onPickClick(): void {
    this.picking.set(true);
    this.result.set(null);

    if (this.context() === 'records') {
      this.discogsService.getRandomRelease({
        decade: this.decadeFilter() || undefined,
        format: this.formatFilter() || undefined,
        genre: this.genreFilter() || undefined,
      }).subscribe({
        next: (release) => {
          this.result.set(release);
          this.picking.set(false);
        },
        error: () => this.picking.set(false),
      });
    } else {
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
        this.result.set(filtered[randomIndex]);
      }
      this.picking.set(false);
    }
  }

  onViewDetailsClick(): void {
    const r = this.result();
    if (this.isRelease(r)) {
      this.router.navigate(['/releases', r.id]);
    }
  }
}
