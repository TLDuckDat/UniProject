import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { provideApiConfiguration } from '../../api/api-configuration';
import { NguoiDungResponse, TokenResponse } from '../../api/models';
import { authInterceptor, CO_SO_HEADER } from './auth.interceptor';
import { AuthService } from './auth.service';

const nguoiDung: NguoiDungResponse = {
  id: 1,
  tenDangNhap: 'pdt.hn',
  vaiTro: [],
  quyen: ['Dot.QuanLy'],
  coSoDuocPhep: [{ id: 1, maCoSo: 'HN', tenCoSo: 'Hà Nội' }],
};

const token = (accessToken: string): TokenResponse => ({
  accessToken,
  hetHanLuc: '2026-11-08T20:15:00+07:00',
  nguoiDung,
});

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let auth: AuthService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideApiConfiguration(''),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => httpTesting.verify());

  function dangNhap(accessToken = 'cu'): void {
    auth.dangNhap('pdt.hn', 'x').subscribe();
    httpTesting.expectOne('/api/v1/auth/login').flush(token(accessToken));
  }

  it('gắn Bearer token và X-CoSo-Id cho request /api', () => {
    dangNhap('abc');

    http.get('/api/v1/sinh-vien').subscribe();

    const req = httpTesting.expectOne('/api/v1/sinh-vien');
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc');
    expect(req.request.headers.get(CO_SO_HEADER)).toBe('1');
    req.flush({});
  });

  it('không gắn header cho URL ngoài /api', () => {
    dangNhap();

    http.get('/assets/x.json').subscribe();

    expect(httpTesting.expectOne('/assets/x.json').request.headers.has('Authorization')).toBe(
      false,
    );
  });

  it('nhiều request cùng 401 → chỉ refresh MỘT lần rồi gửi lại với token mới', () => {
    dangNhap('cu');
    const ketQua: unknown[] = [];

    http.get('/api/v1/a').subscribe((r) => ketQua.push(r));
    http.get('/api/v1/b').subscribe((r) => ketQua.push(r));
    httpTesting.expectOne('/api/v1/a').flush(null, { status: 401, statusText: 'Unauthorized' });
    httpTesting.expectOne('/api/v1/b').flush(null, { status: 401, statusText: 'Unauthorized' });

    const refresh = httpTesting.match('/api/v1/auth/refresh');
    expect(refresh.length).toBe(1);
    refresh[0].flush(token('moi'));

    const a = httpTesting.expectOne('/api/v1/a');
    const b = httpTesting.expectOne('/api/v1/b');
    expect(a.request.headers.get('Authorization')).toBe('Bearer moi');
    expect(b.request.headers.get('Authorization')).toBe('Bearer moi');
    a.flush('A');
    b.flush('B');
    expect(ketQua).toEqual(['A', 'B']);
  });

  it('request mới trong lúc đang refresh thì chờ refresh xong mới gửi', () => {
    dangNhap('cu');
    http.get('/api/v1/a').subscribe();
    httpTesting.expectOne('/api/v1/a').flush(null, { status: 401, statusText: 'Unauthorized' });

    http.get('/api/v1/c').subscribe();
    httpTesting.expectNone('/api/v1/c');

    httpTesting.expectOne('/api/v1/auth/refresh').flush(token('moi'));
    expect(httpTesting.expectOne('/api/v1/c').request.headers.get('Authorization')).toBe(
      'Bearer moi',
    );
    httpTesting.expectOne('/api/v1/a').flush({});
  });

  it('refresh thất bại → hết phiên, về trang đăng nhập và trả lỗi 401 gốc', () => {
    dangNhap('cu');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    let loi: { status?: number } | undefined;

    http.get('/api/v1/a').subscribe({ error: (e: { status: number }) => (loi = e) });
    httpTesting.expectOne('/api/v1/a').flush(null, { status: 401, statusText: 'Unauthorized' });
    httpTesting
      .expectOne('/api/v1/auth/refresh')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(loi?.status).toBe(401);
    expect(auth.daDangNhap()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/dang-nhap'], expect.anything());
  });

  it('401 của chính endpoint đăng nhập không kích hoạt refresh', () => {
    let loi = false;
    auth.dangNhap('x', 'sai').subscribe({ error: () => (loi = true) });

    httpTesting
      .expectOne('/api/v1/auth/login')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    httpTesting.expectNone('/api/v1/auth/refresh');
    expect(loi).toBe(true);
  });
});
