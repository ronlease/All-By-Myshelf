import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatToolbarModule } from '@angular/material/toolbar';
import { FeaturesDto, FeaturesService } from '../core/config/features.service';
import { SyncStateService } from '../core/sync/sync-state.service';
import { ThemeService } from '../core/config/theme.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    MatButtonModule,
    MatDividerModule,
    MatIconModule,
    MatListModule,
    MatProgressSpinnerModule,
    MatSidenavModule,
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
  private readonly themeService = inject(ThemeService);

  ngOnInit(): void {
    this.themeService.initialize();
    this.featuresService.getFeatures().subscribe({
      next: (f) => this.features.set(f),
    });
  }

  syncAll(): void {
    this.syncState.startDiscogsSync();
    this.syncState.startBooksSync();
  }
}
