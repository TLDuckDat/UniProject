import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  provideRouter,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';

import { AuthService } from './auth.service';
import { authGuard, khachGuard, roleGuard } from './guards';

describe('guards', () => {
  const daDangNhap = signal(false);
  let quyen: string[] = [];

  beforeEach(() => {
    daDangNhap.set(false);
    quyen = [];
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            daDangNhap,
            coQuyen: (...q: string[]) => q.some((x) => quyen.includes(x)),
            coVaiTro: () => false,
          },
        },
      ],
    });
  });

  const chay = (fn: () => unknown) => TestBed.runInInjectionContext(fn);
  const url = (v: unknown) => TestBed.inject(Router).serializeUrl(v as UrlTree);
  const route = (data: Record<string, unknown>) => ({ data }) as unknown as ActivatedRouteSnapshot;

  it('authGuard: chưa đăng nhập → /dang-nhap kèm returnUrl', () => {
    const kq = chay(() => authGuard(route({}), { url: '/de-tai' } as RouterStateSnapshot));

    expect(url(kq)).toBe('/dang-nhap?returnUrl=%2Fde-tai');
  });

  it('authGuard: đã đăng nhập → cho qua', () => {
    daDangNhap.set(true);

    expect(chay(() => authGuard(route({}), { url: '/' } as RouterStateSnapshot))).toBe(true);
  });

  it('khachGuard: đã đăng nhập thì không vào trang đăng nhập nữa', () => {
    daDangNhap.set(true);

    expect(url(chay(() => khachGuard(route({}), {} as RouterStateSnapshot)))).toBe('/');
  });

  it('roleGuard: thiếu quyền → /403', () => {
    quyen = ['SinhVien.Xem'];

    const kq = chay(() => roleGuard(route({ quyen: ['Dot.QuanLy'] }), {} as RouterStateSnapshot));

    expect(url(kq)).toBe('/403');
  });

  it('roleGuard: có MỘT trong các quyền → cho qua', () => {
    quyen = ['DeTai.Duyet'];

    const kq = chay(() =>
      roleGuard(route({ quyen: ['DeTai.DeXuat', 'DeTai.Duyet'] }), {} as RouterStateSnapshot),
    );

    expect(kq).toBe(true);
  });
});
