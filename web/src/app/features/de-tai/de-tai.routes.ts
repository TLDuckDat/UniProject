import { Routes } from '@angular/router';

export const DE_TAI_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./de-tai.page').then((m) => m.DeTaiPage),
  },
];
