import { Injectable, signal } from '@angular/core';

type Theme = 'light' | 'dark' | 'os-default';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly STORAGE_KEY = 'app-theme';

  readonly theme = signal<Theme>('os-default');

  applyTheme(theme: Theme): void {
    this.theme.set(theme);
    localStorage.setItem(this.STORAGE_KEY, theme);

    const html = document.documentElement;

    if (theme === 'light') {
      html.classList.add('light-theme');
      html.classList.remove('dark-theme');
    } else if (theme === 'dark') {
      html.classList.add('dark-theme');
      html.classList.remove('light-theme');
    } else {
      html.classList.remove('light-theme', 'dark-theme');
    }
  }

  initialize(osDefault: 'light' | 'dark' = 'light'): void {
    const stored = localStorage.getItem(this.STORAGE_KEY) as Theme | null;
    if (stored && ['light', 'dark', 'os-default'].includes(stored)) {
      this.applyTheme(stored);
    } else {
      this.applyTheme(osDefault);
    }
  }
}
