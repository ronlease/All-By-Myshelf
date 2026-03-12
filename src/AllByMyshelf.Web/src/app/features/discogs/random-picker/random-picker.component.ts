import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatToolbarModule } from '@angular/material/toolbar';
import { DiscogsService, ReleaseDetailDto, ReleaseDto } from '../discogs.service';
import { FormatIconPipe } from '../format-icon.pipe';

@Component({
  selector: 'app-random-picker',
  standalone: true,
  imports: [
    FormatIconPipe,
    FormsModule,
    MatButtonModule,
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
  private readonly discogsService = inject(DiscogsService);
  private readonly router = inject(Router);

  allReleases = signal<ReleaseDto[]>([]);
  decadeFilter = signal('');
  formatFilter = signal('');
  genreFilter = signal('');
  loading = signal(true);
  picking = signal(false);
  result = signal<ReleaseDetailDto | null>(null);

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

  ngOnInit(): void {
    this.discogsService.getCollection(1, 10000).subscribe({
      next: (res) => {
        this.allReleases.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onBackClick(): void {
    this.router.navigate(['/']);
  }

  onPickClick(): void {
    this.picking.set(true);
    this.result.set(null);
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
  }

  onViewDetailsClick(): void {
    const r = this.result();
    if (r) this.router.navigate(['/releases', r.id]);
  }
}
