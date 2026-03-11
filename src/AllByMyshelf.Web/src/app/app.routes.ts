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
    path: 'releases/:id',
    canActivate: [authGuardFn],
    loadComponent: () =>
      import('./features/discogs/release-detail/release-detail.component').then(
        (m) => m.ReleaseDetailComponent,
      ),
  },
  {
    path: '**',
    redirectTo: '',
  },
];
