import { inject } from '@angular/core';
import {
  ActivatedRouteSnapshot,
  CanActivateFn,
  Router,
  RouterStateSnapshot,
} from '@angular/router';

import { AuthService } from './auth.service';

/** Dữ liệu route khai báo quyền: `data: { quyen: ['Dot.QuanLy'] }` (có MỘT trong các quyền là đủ). */
export interface RouteQuyenData {
  quyen?: string[];
  vaiTro?: string[];
}

/** Bắt buộc đăng nhập; chưa đăng nhập → /dang-nhap?returnUrl=… */
export const authGuard: CanActivateFn = (
  _route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot,
) => {
  const auth = inject(AuthService);
  if (auth.daDangNhap()) {
    return true;
  }
  return inject(Router).createUrlTree(['/dang-nhap'], {
    queryParams: state.url && state.url !== '/' ? { returnUrl: state.url } : {},
  });
};

/** Trang đăng nhập: đã đăng nhập thì về trang chủ. */
export const khachGuard: CanActivateFn = () =>
  inject(AuthService).daDangNhap() ? inject(Router).createUrlTree(['/']) : true;

/** Kiểm quyền/vai trò khai báo ở `route.data`; không đủ → trang 403. */
export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const auth = inject(AuthService);
  const { quyen, vaiTro } = route.data as RouteQuyenData;
  const datQuyen = !quyen?.length || auth.coQuyen(...quyen);
  const datVaiTro = !vaiTro?.length || auth.coVaiTro(...vaiTro);
  return datQuyen && datVaiTro ? true : inject(Router).createUrlTree(['/403']);
};
