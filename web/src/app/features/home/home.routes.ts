import { Routes } from '@angular/router';

export const HOME_ROUTES: Routes = [
  {
    path: '',
    title: 'Trang chủ – UPM',
    loadComponent: () => import('./home.page').then((m) => m.HomePage),
  },
];
