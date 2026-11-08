import { Routes } from '@angular/router';

/** Mỗi feature lazy-load qua file `*.routes.ts` của chính nó. */
export const routes: Routes = [
  {
    path: '',
    loadChildren: () => import('./features/home/home.routes').then((m) => m.HOME_ROUTES),
  },
  { path: '**', redirectTo: '' },
];
