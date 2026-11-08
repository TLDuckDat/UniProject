import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  {
    path: '',
    title: 'Đăng nhập – UPM',
    loadComponent: () => import('./login.page').then((m) => m.LoginPage),
  },
];
