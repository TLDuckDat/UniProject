import { Routes } from '@angular/router';

export const DOT_DATN_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./danh-sach-dot.page').then((m) => m.DanhSachDotPage),
  },
  {
    path: ':id',
    title: 'Chi tiết đợt ĐATN – UPM',
    loadComponent: () => import('./chi-tiet-dot.page').then((m) => m.ChiTietDotPage),
  },
];
