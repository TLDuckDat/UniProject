import { Routes } from '@angular/router';

export const HO_SO_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./ket-qua-xet-giao.page').then((m) => m.KetQuaXetGiaoPage),
  },
];
