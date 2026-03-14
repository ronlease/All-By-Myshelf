import { Component, computed, inject, OnInit, signal } from '@angular/core';
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
  themeIcon = computed(() => {
    const current = this.themeService.theme();
    return current === 'dark' ? 'light_mode' : 'dark_mode';
  });
  readonly themeService = inject(ThemeService);

  cycleTheme(): void {
    const current = this.themeService.theme();
    const nextTheme = current === 'dark' ? 'light' : 'dark';
    this.themeService.applyTheme(nextTheme);
  }

  ngOnInit(): void {
    this.themeService.initialize(this.detectOsTheme());
    this.featuresService.getFeatures().subscribe({
      next: (f) => this.features.set(f),
    });
  }

  private detectOsTheme(): 'light' | 'dark' {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  syncAll(): void {
    this.syncState.startBoardGamesSync();
    this.syncState.startDiscogsSync();
    this.syncState.startBooksSync();
  }
}
