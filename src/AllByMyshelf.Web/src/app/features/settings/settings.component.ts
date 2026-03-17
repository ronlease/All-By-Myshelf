import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { FeaturesService } from '../../core/config/features.service';
import { SettingsDto, SettingsService, UpdateSettingsDto } from '../../core/config/settings.service';
import { ThemeService } from '../../core/config/theme.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    ReactiveFormsModule,
  ],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent implements OnInit {
  bggApiTokenControl = new FormControl<string>('');
  bggUsernameControl = new FormControl<string>('');
  discogsPersonalAccessTokenControl = new FormControl<string>('');
  discogsUsernameControl = new FormControl<string>('');
  private readonly featuresService = inject(FeaturesService);
  hardcoverApiTokenControl = new FormControl<string>('');
  loading = signal<boolean>(true);
  saving = signal<boolean>(false);
  settings = signal<SettingsDto | null>(null);
  private readonly settingsService = inject(SettingsService);
  private readonly snackBar = inject(MatSnackBar);
  themeControl = new FormControl<string>('os-default');
  private readonly themeService = inject(ThemeService);

  private loadSettings(): void {
    this.loading.set(true);
    this.settingsService.getSettings().subscribe({
      next: (s) => {
        this.settings.set(s);
        this.bggApiTokenControl.setValue(s.bggApiToken || '');
        this.bggUsernameControl.setValue(s.bggUsername || '');
        this.discogsPersonalAccessTokenControl.setValue(s.discogsPersonalAccessToken || '');
        this.discogsUsernameControl.setValue(s.discogsUsername || '');
        this.hardcoverApiTokenControl.setValue(s.hardcoverApiToken || '');
        this.themeControl.setValue(s.theme || 'os-default', { emitEvent: false });
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load settings', 'Close', { duration: 5000 });
      },
    });
  }

  ngOnInit(): void {
    this.loadSettings();
  }

  onSaveTokens(): void {
    this.saving.set(true);

    const dto: UpdateSettingsDto = {};
    const current = this.settings();

    const bggApiToken = this.bggApiTokenControl.value?.trim();
    if (bggApiToken && bggApiToken !== current?.bggApiToken) {
      dto.bggApiToken = bggApiToken;
    }

    const bggUsername = this.bggUsernameControl.value?.trim();
    if (bggUsername && bggUsername !== current?.bggUsername) {
      dto.bggUsername = bggUsername;
    }

    const discogsToken = this.discogsPersonalAccessTokenControl.value?.trim();
    if (discogsToken && discogsToken !== current?.discogsPersonalAccessToken) {
      dto.discogsPersonalAccessToken = discogsToken;
    }

    const discogsUsername = this.discogsUsernameControl.value?.trim();
    if (discogsUsername && discogsUsername !== current?.discogsUsername) {
      dto.discogsUsername = discogsUsername;
    }

    const hardcoverToken = this.hardcoverApiTokenControl.value?.trim();
    if (hardcoverToken && hardcoverToken !== current?.hardcoverApiToken) {
      dto.hardcoverApiToken = hardcoverToken;
    }

    this.settingsService.updateSettings(dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.snackBar.open('Settings saved', 'Close', { duration: 3000 });
        this.bggApiTokenControl.reset();
        this.bggUsernameControl.reset();
        this.discogsPersonalAccessTokenControl.reset();
        this.discogsUsernameControl.reset();
        this.hardcoverApiTokenControl.reset();
        this.featuresService.refresh();
        this.loadSettings();
      },
      error: () => {
        this.saving.set(false);
        this.snackBar.open('Failed to save settings', 'Close', { duration: 5000 });
      },
    });
  }

  onThemeChange(theme: string): void {
    this.themeService.applyTheme(theme as 'light' | 'dark' | 'os-default');

    const dto: UpdateSettingsDto = { theme };
    this.settingsService.updateSettings(dto).subscribe({
      next: () => {
        this.snackBar.open('Theme saved', 'Close', { duration: 2000 });
      },
      error: () => {
        this.snackBar.open('Failed to save theme preference', 'Close', { duration: 5000 });
      },
    });
  }

}
