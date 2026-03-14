import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DiscogsService, DuplicateGroupDto } from '../discogs.service';

@Component({
  selector: 'app-duplicates',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatExpansionModule,
    MatListModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  templateUrl: './duplicates.component.html',
  styleUrl: './duplicates.component.scss',
})
export class DuplicatesComponent implements OnInit {
  private readonly discogsService = inject(DiscogsService);
  duplicateGroups = signal<DuplicateGroupDto[]>([]);
  loading = signal(true);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  ngOnInit(): void {
    this.discogsService.getDuplicates().subscribe({
      next: (groups) => {
        this.duplicateGroups.set(groups);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load duplicates.', 'Dismiss', { duration: 5000 });
      },
    });
  }

  onReleaseClick(id: string): void {
    this.router.navigate(['/releases', id]);
  }
}
