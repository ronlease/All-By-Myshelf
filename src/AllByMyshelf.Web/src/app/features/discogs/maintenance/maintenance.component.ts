import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { DiscogsService, MaintenanceReleaseDto } from '../discogs.service';

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
    MatTableModule,
  ],
  templateUrl: './maintenance.component.html',
  styleUrl: './maintenance.component.scss',
})
export class MaintenanceComponent implements OnInit {
  readonly displayedColumns = ['thumbnail', 'artist', 'title', 'missingFields', 'discogs'];
  private readonly discogsService = inject(DiscogsService);
  loading = signal(true);
  releases = signal<MaintenanceReleaseDto[]>([]);
  private readonly snackBar = inject(MatSnackBar);

  ngOnInit(): void {
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
}
