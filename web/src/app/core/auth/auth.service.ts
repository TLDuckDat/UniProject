import { HttpContext } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  catchError,
  finalize,
  firstValueFrom,
  map,
  Observable,
  shareReplay,
  tap,
  throwError,
} from 'rxjs';

import { CoSoResponse, NguoiDungResponse, TokenResponse } from '../../api/models';
import { XacThucService } from '../../api/services';
import { SKIP_ERROR_MESSAGE } from '../http/problem-details.interceptor';

const COSO_KEY = 'upm.coSoDangLamViec';

/**
 * Phiên đăng nhập phía client.
 * - Access token chỉ giữ TRONG BỘ NHỚ (biến private) – không localStorage/sessionStorage → script lạ (XSS) khó lấy.
 * - Refresh token nằm trong cookie HttpOnly do server đặt (Path=/api/v1/auth, SameSite=Strict) – JS không đọc được.
 * - Tải lại trang: gọi /auth/refresh (cookie tự gửi) để khôi phục phiên.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(XacThucService);
  private readonly router = inject(Router);

  private accessToken: string | null = null;
  private dangLamMoi$: Observable<string> | null = null;

  private readonly _nguoiDung = signal<NguoiDungResponse | null>(null);
  private readonly _coSoChonId = signal<number | null>(docCoSoDaLuu());

  readonly nguoiDung = this._nguoiDung.asReadonly();
  readonly daDangNhap = computed(() => this._nguoiDung() !== null);
  readonly quyen = computed(() => new Set(this._nguoiDung()?.quyen ?? []));
  readonly coSoDuocPhep = computed<CoSoResponse[]>(() => this._nguoiDung()?.coSoDuocPhep ?? []);

  /** Cơ sở đang làm việc: lựa chọn đã lưu nếu còn hợp lệ, nếu không thì cơ sở đầu tiên được phép. */
  readonly coSoDangLamViec = computed<CoSoResponse | null>(() => {
    const ds = this.coSoDuocPhep();
    return ds.find((c) => c.id === this._coSoChonId()) ?? ds[0] ?? null;
  });

  get token(): string | null {
    return this.accessToken;
  }

  get dangLamMoiToken(): boolean {
    return this.dangLamMoi$ !== null;
  }

  coQuyen(...quyen: string[]): boolean {
    const co = this.quyen();
    return quyen.some((q) => co.has(q));
  }

  coVaiTro(...maVaiTro: string[]): boolean {
    return (this._nguoiDung()?.vaiTro ?? []).some((v) => maVaiTro.includes(v.maVaiTro));
  }

  dangNhap(tenDangNhap: string, matKhau: string): Observable<void> {
    return this.api
      .dangNhap({ body: { tenDangNhap, matKhau } })
      .pipe(map((res) => this.nhanToken(res)));
  }

  /**
   * Lấy access token mới bằng refresh token (cookie). Nhiều request 401 cùng lúc dùng chung MỘT lần gọi
   * (shareReplay) – không refresh trùng.
   */
  lamMoiToken(): Observable<string> {
    this.dangLamMoi$ ??= this.api
      .lamMoi(undefined, new HttpContext().set(SKIP_ERROR_MESSAGE, true))
      .pipe(
        map((res) => {
          this.nhanToken(res);
          return res.accessToken;
        }),
        catchError((err: unknown) => {
          this.xoaPhien();
          return throwError(() => err);
        }),
        finalize(() => (this.dangLamMoi$ = null)),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
    return this.dangLamMoi$;
  }

  /** Gọi khi khởi động app: thử khôi phục phiên từ cookie refresh; không có phiên thì bỏ qua. */
  async khoiPhucPhien(): Promise<void> {
    try {
      await firstValueFrom(this.lamMoiToken());
    } catch {
      // Chưa đăng nhập hoặc phiên hết hạn – guard sẽ chuyển tới trang đăng nhập.
    }
  }

  dangXuat(): Observable<void> {
    return this.api.dangXuat(undefined, new HttpContext().set(SKIP_ERROR_MESSAGE, true)).pipe(
      catchError(() => [undefined]),
      tap(() => this.hetPhien(false)),
      map(() => undefined),
    );
  }

  /** Phiên không còn hợp lệ: xóa trạng thái và về trang đăng nhập (giữ đường dẫn để quay lại). */
  hetPhien(giuDuongDan = true): void {
    this.xoaPhien();
    const returnUrl = giuDuongDan ? this.router.url : undefined;
    void this.router.navigate(['/dang-nhap'], {
      queryParams: returnUrl && returnUrl !== '/' ? { returnUrl } : {},
    });
  }

  chonCoSo(coSoId: number): void {
    this._coSoChonId.set(coSoId);
    try {
      localStorage.setItem(COSO_KEY, String(coSoId)); // chỉ là tùy chọn giao diện, không nhạy cảm
    } catch {
      // trình duyệt chặn storage – bỏ qua
    }
  }

  private nhanToken(res: TokenResponse): void {
    this.accessToken = res.accessToken;
    this._nguoiDung.set(res.nguoiDung);
  }

  private xoaPhien(): void {
    this.accessToken = null;
    this._nguoiDung.set(null);
  }
}

function docCoSoDaLuu(): number | null {
  try {
    const v = Number(localStorage.getItem(COSO_KEY));
    return Number.isFinite(v) && v > 0 ? v : null;
  } catch {
    return null;
  }
}
