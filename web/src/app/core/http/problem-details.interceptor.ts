import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { catchError, throwError } from 'rxjs';

import { toVietnameseMessage } from './problem-details';

/**
 * Đặt `true` trong HttpContext khi nơi gọi tự hiển thị lỗi (vd. form hiển thị lỗi theo trường):
 * `http.get(url, { context: new HttpContext().set(SKIP_ERROR_MESSAGE, true) })`.
 */
export const SKIP_ERROR_MESSAGE = new HttpContextToken<boolean>(() => false);

/**
 * Bắt mọi lỗi HTTP, đọc ProblemDetails từ API và hiển thị `nz-message` tiếng Việt.
 * Lỗi vẫn được ném tiếp để nơi gọi xử lý trạng thái (loading, form…).
 */
export const problemDetailsInterceptor: HttpInterceptorFn = (req, next) => {
  const message = inject(NzMessageService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && !req.context.get(SKIP_ERROR_MESSAGE)) {
        message.error(toVietnameseMessage(error), { nzDuration: 5000 });
      }
      return throwError(() => error);
    }),
  );
};
