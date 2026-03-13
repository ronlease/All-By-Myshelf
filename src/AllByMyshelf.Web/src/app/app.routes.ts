import { Routes } from '@angular/router';
import { authGuardFn } from '@auth0/auth0-angular';
import { AppShellComponent } from './layout/app-shell.component';

export const routes: Routes = [
  {
    path: 'callback',
    redirectTo: '',
  },
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuardFn],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/discogs/collection/collection.component').then(
            (m) => m.CollectionComponent,
          ),
      },
      {
        path: 'books',
        loadComponent: () =>
          import('./features/hardcover/books/books.component').then(
            (m) => m.BooksComponent,
          ),
      },
      {
        path: 'maintenance',
        loadComponent: () =>
          import('./features/discogs/maintenance/maintenance.component').then(
            (m) => m.MaintenanceComponent,
          ),
      },
      {
        path: 'pick',
        loadComponent: () =>
          import('./features/discogs/random-picker/random-picker.component').then(
            (m) => m.RandomPickerComponent,
          ),
      },
      {
        path: 'releases/:id',
        loadComponent: () =>
          import('./features/discogs/release-detail/release-detail.component').then(
            (m) => m.ReleaseDetailComponent,
          ),
      },
      {
        path: 'statistics',
        loadComponent: () =>
          import('./features/statistics/statistics.component').then(
            (m) => m.StatisticsComponent,
          ),
      },
      {
        path: 'stores',
        loadComponent: () =>
          import('./features/store-finder/store-finder.component').then(
            (m) => m.StoreFinderComponent,
          ),
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
