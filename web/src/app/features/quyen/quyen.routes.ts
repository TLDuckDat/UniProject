import { Routes } from '@angular/router';

export const QUYEN_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./quyen.page').then((m) => m.QuyenPage),
  },
];
