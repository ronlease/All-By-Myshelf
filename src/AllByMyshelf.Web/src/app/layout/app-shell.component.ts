import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FeaturesDto, FeaturesService } from '../core/config/features.service';
import { SyncStateService } from '../core/sync/sync-state.service';
import { ThemeService } from '../core/config/theme.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatToolbarModule,
    MatTooltipModule,
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
}
