import { Routes } from '@angular/router';
import { authGuardFn } from '@auth0/auth0-angular';

export const routes: Routes = [
  {
    path: 'callback',
    redirectTo: '',
  },
  {
    path: '',
    canActivate: [authGuardFn],
    loadComponent: () =>
      import('./features/discogs/collection/collection.component').then(
        (m) => m.CollectionComponent,
      ),
  },
  {
    path: 'pick',
    canActivate: [authGuardFn],
    loadComponent: () =>
      import('./features/discogs/random-picker/random-picker.component').then(
        (m) => m.RandomPickerComponent,
      ),
  },
  {
    path: 'releases/:id',
    canActivate: [authGuardFn],
    loadComponent: () =>
      import('./features/discogs/release-detail/release-detail.component').then(
        (m) => m.ReleaseDetailComponent,
      ),
  },
  {
    path: 'statistics',
    canActivate: [authGuardFn],
    loadComponent: () =>
      import('./features/statistics/statistics.component').then(
        (m) => m.StatisticsComponent,
      ),
  },
  {
    path: 'stores',
    canActivate: [authGuardFn],
    loadComponent: () =>
      import('./features/store-finder/store-finder.component').then(
        (m) => m.StoreFinderComponent,
      ),
  },
  {
    path: '**',
    redirectTo: '',
  },
];
