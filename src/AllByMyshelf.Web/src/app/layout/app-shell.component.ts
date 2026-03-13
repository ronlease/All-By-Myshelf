import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { FeaturesDto, FeaturesService } from '../core/config/features.service';
import { SyncStateService } from '../core/sync/sync-state.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    MatButtonModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatToolbarModule,
    RouterModule,
  ],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss',
})
export class AppShellComponent implements OnInit {
  features = signal<FeaturesDto | null>(null);
  private readonly featuresService = inject(FeaturesService);
  readonly syncState = inject(SyncStateService);

  ngOnInit(): void {
    this.featuresService.getFeatures().subscribe({
      next: (f) => this.features.set(f),
    });
  }
}
