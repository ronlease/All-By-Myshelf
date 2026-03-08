import { Routes } from '@angular/router';
import { authGuardFn } from '@auth0/auth0-angular';

export const routes: Routes = [
  {
    path: '',
    canActivate: [authGuardFn],
    loadComponent: () =>
      import('./features/discogs/collection/collection.component').then(
        (m) => m.CollectionComponent,
      ),
  },
  {
    path: '**',
    redirectTo: '',
  },
];
