import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, defer, switchMap, throwError } from 'rxjs';

import { AuthService } from './auth.service';

/** Header chọn cơ sở đang làm việc (server chỉ chấp nhận cơ sở trong phạm vi được phép). */
export const CO_SO_HEADER = 'X-CoSo-Id';

/** Endpoint xác thực: không tự refresh khi 401 (login sai mật khẩu, refresh hết hạn…). */
const AUTH_KHONG_REFRESH = /\/api\/v1\/auth\/(login|refresh|logout)$/;

/**
 * Gắn `Authorization: Bearer` + `X-CoSo-Id` cho request /api; gặp 401 thì refresh token MỘT lần
 * (các request đồng thời xếp hàng chờ chung lần refresh đó) rồi gửi lại. Refresh thất bại → hết phiên.
 *
 * Thứ tự trong `withInterceptors`: đặt SAU problemDetailsInterceptor để 401 được xử lý (refresh) trước
 * khi tới bước hiển thị lỗi.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/')) {
    return next(req);
  }

  const auth = inject(AuthService);
  // defer: đọc token tại thời điểm gửi (sau refresh sẽ là token mới).
  const gui = () => defer(() => next(ganHeader(req, auth)));

  if (AUTH_KHONG_REFRESH.test(req.url)) {
    return gui();
  }

  // Đang refresh (do request khác gặp 401) → chờ xong rồi mới gửi, tránh 401 dây chuyền.
  const truocKhiGui = auth.dangLamMoiToken
    ? auth.lamMoiToken().pipe(switchMap(() => gui()))
    : gui();

  return truocKhiGui.pipe(
    catchError((err: unknown) => {
      if (!(err instanceof HttpErrorResponse) || err.status !== 401) {
        return throwError(() => err);
      }
      return auth.lamMoiToken().pipe(
        catchError(() => {
          auth.hetPhien();
          return throwError(() => err);
        }),
        switchMap(() => gui()),
      );
    }),
  );
};

function ganHeader(req: HttpRequest<unknown>, auth: AuthService): HttpRequest<unknown> {
  const headers: Record<string, string> = {};
  if (auth.token) {
    headers['Authorization'] = `Bearer ${auth.token}`;
  }
  const coSo = auth.coSoDangLamViec();
  if (coSo) {
    headers[CO_SO_HEADER] = String(coSo.id);
  }
  return req.clone({ setHeaders: headers });
}
