import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatToolbarModule } from '@angular/material/toolbar';
import { DiscogsService, ReleaseDetailDto } from '../discogs.service';
import { FormatIconPipe } from '../format-icon.pipe';

@Component({
  selector: 'app-release-detail',
  standalone: true,
  imports: [
    FormatIconPipe,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatListModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatToolbarModule,
    ReactiveFormsModule,
    RouterModule,
  ],
  templateUrl: './release-detail.component.html',
  styleUrl: './release-detail.component.scss',
})
export class ReleaseDetailComponent implements OnInit {
  private readonly discogsService = inject(DiscogsService);
  error = signal(false);
  loading = signal(true);
  readonly Math = Math;
  notesControl = new FormControl<string>('');
  rating = signal<number | null>(null);
  release = signal<ReleaseDetailDto | null>(null);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  saving = signal<boolean>(false);
  private readonly snackBar = inject(MatSnackBar);

  protected expandArtists(artists: string[]): string[] {
    return artists
      .flatMap(a => a.split(','))
      .map(a => a.replace(/\s*\(\d+\)$/, '').trim())
      .filter(a => a.length > 0);
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set(true);
      this.loading.set(false);
      return;
    }

    this.discogsService.getRelease(id).subscribe({
      next: (detail) => {
        this.release.set(detail);
        this.notesControl.setValue(detail.notes ?? '');
        this.rating.set(detail.rating);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  onBackClick(): void {
    this.router.navigate(['/']);
  }

  saveNotesAndRating(): void {
    const releaseId = this.release()?.id;
    if (!releaseId) return;

    this.saving.set(true);
    this.discogsService.updateNotesAndRating(releaseId, {
      notes: this.notesControl.value || null,
      rating: this.rating(),
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.snackBar.open('Saved!', undefined, { duration: 2000 });
      },
      error: () => {
        this.saving.set(false);
        this.snackBar.open('Failed to save.', 'Dismiss', { duration: 5000 });
      },
    });
  }

  setRating(value: number): void {
    if (this.rating() === value) {
      this.rating.set(null);
    } else {
      this.rating.set(value);
    }
  }
}
