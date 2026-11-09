import { Routes } from '@angular/router';

export const TIEN_DO_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./tien-do.page').then((m) => m.TienDoPage),
  },
];
