import { Routes } from '@angular/router';

import { authGuard, khachGuard, roleGuard } from './core/auth/guards';
import { Quyen } from './core/auth/quyen';
import { MenuData } from './core/layout/menu';

const dangXayDung = () => import('./features/loi/loi.pages').then((m) => m.DangXayDungPage);
const menu = (nhan: string, icon: string, thuTu: number): { menu: MenuData } => ({
  menu: { nhan, icon, thuTu },
});

/**
 * - Mỗi feature lazy-load qua file `*.routes.ts` / `loadComponent`.
 * - Route con của layout có `data.menu` → hiện trên menu; `data.quyen` → lọc menu + `roleGuard`.
 */
export const routes: Routes = [
  {
    path: 'dang-nhap',
    canActivate: [khachGuard],
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./core/layout/main-layout').then((m) => m.MainLayout),
    children: [
      {
        path: '',
        pathMatch: 'full',
        data: menu('Trang chủ', 'home', 0),
        loadChildren: () => import('./features/home/home.routes').then((m) => m.HOME_ROUTES),
      },
      // ----- Module nghiệp vụ: tạm trỏ trang "Đang xây dựng" (thay ở Phần 6–13) -----
      {
        path: 'dot-datn',
        title: 'Đợt ĐATN – UPM',
        canActivate: [roleGuard],
        data: {
          ...menu('Đợt ĐATN & xét điều kiện', 'calendar', 10),
          quyen: [Quyen.DotQuanLy, Quyen.XetDieuKienChay],
        },
        loadComponent: dangXayDung,
      },
      {
        path: 'de-tai',
        title: 'Đề tài – UPM',
        canActivate: [roleGuard],
        data: {
          ...menu('Đề tài & phân công', 'file-text', 20),
          quyen: [Quyen.DeTaiDeXuat, Quyen.DeTaiDuyet, Quyen.PhanCongQuanLy],
        },
        loadComponent: dangXayDung,
      },
      {
        path: 'hoi-dong',
        title: 'Hội đồng – UPM',
        canActivate: [roleGuard],
        data: {
          ...menu('Hội đồng & bảo vệ', 'team', 30),
          quyen: [Quyen.HoiDongDeXuat, Quyen.LichBaoVeXep],
        },
        loadComponent: dangXayDung,
      },
      {
        path: 'ho-so',
        title: 'Hồ sơ ĐATN của tôi – UPM',
        canActivate: [roleGuard],
        data: { ...menu('Hồ sơ ĐATN của tôi', 'profile', 40), quyen: [Quyen.HoSoCuaToi] },
        loadComponent: dangXayDung,
      },
      {
        path: 'quan-tri',
        title: 'Quản trị – UPM',
        canActivate: [roleGuard],
        data: {
          ...menu('Quản trị hệ thống', 'setting', 80),
          quyen: [Quyen.TaiKhoanQuanLy, Quyen.RuleSetQuanLy, Quyen.DanhMucQuanLy],
        },
        loadComponent: dangXayDung,
      },
      {
        path: 'tai-khoan',
        title: 'Tài khoản của tôi – UPM',
        data: menu('Tài khoản của tôi', 'user', 90),
        loadComponent: () =>
          import('./features/tai-khoan/tai-khoan.page').then((m) => m.TaiKhoanPage),
      },
      {
        path: '403',
        title: 'Không có quyền – UPM',
        loadComponent: () => import('./features/loi/loi.pages').then((m) => m.ForbiddenPage),
      },
      {
        path: '**',
        title: 'Không tìm thấy – UPM',
        loadComponent: () => import('./features/loi/loi.pages').then((m) => m.NotFoundPage),
      },
    ],
  },
];
