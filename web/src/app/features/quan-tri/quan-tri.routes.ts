import { Routes } from '@angular/router';

export const QUAN_TRI_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./quan-tri-layout').then((m) => m.QuanTriLayout),
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./danh-muc.page').then((m) => m.DanhMucPage),
      },
      {
        path: 'rule-set',
        loadComponent: () => import('./rule-set.page').then((m) => m.RuleSetPage),
      },
      { path: 'import', loadComponent: () => import('./import.page').then((m) => m.ImportPage) },
      {
        path: 'tai-khoan',
        loadComponent: () => import('./tai-khoan-quan-ly.page').then((m) => m.TaiKhoanQuanLyPage),
      },
    ],
  },
];
