import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NzMessageService } from 'ng-zorro-antd/message';

import { NETWORK_ERROR_MESSAGE } from './problem-details';
import { problemDetailsInterceptor, SKIP_ERROR_MESSAGE } from './problem-details.interceptor';

describe('problemDetailsInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  const messageError = vi.fn();

  beforeEach(() => {
    messageError.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([problemDetailsInterceptor])),
        provideHttpClientTesting(),
        { provide: NzMessageService, useValue: { error: messageError } },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('hiển thị detail của ProblemDetails nghiệp vụ', () => {
    http.get('/api/v1/x').subscribe({ error: () => undefined });

    httpTesting.expectOne('/api/v1/x').flush(
      {
        status: 422,
        title: 'Vi phạm quy định nghiệp vụ.',
        detail: 'Sinh viên nợ quá 10 tín chỉ.',
        code: 'UPM_A02_NO_QUA_10_TC',
      },
      { status: 422, statusText: 'Unprocessable Entity' },
    );

    expect(messageError).toHaveBeenCalledWith('Sinh viên nợ quá 10 tín chỉ.', expect.anything());
  });

  it('dùng title khi không có detail', () => {
    http.get('/api/v1/x').subscribe({ error: () => undefined });

    httpTesting
      .expectOne('/api/v1/x')
      .flush(
        { status: 404, title: 'Không tìm thấy tài nguyên yêu cầu.', code: 'UPM_NOT_FOUND' },
        { status: 404, statusText: 'Not Found' },
      );

    expect(messageError).toHaveBeenCalledWith(
      'Không tìm thấy tài nguyên yêu cầu.',
      expect.anything(),
    );
  });

  it('báo mất kết nối khi status = 0', () => {
    http.get('/api/v1/x').subscribe({ error: () => undefined });

    httpTesting.expectOne('/api/v1/x').error(new ProgressEvent('error'));

    expect(messageError).toHaveBeenCalledWith(NETWORK_ERROR_MESSAGE, expect.anything());
  });

  it('không hiển thị khi nơi gọi đặt SKIP_ERROR_MESSAGE, nhưng vẫn ném lỗi', () => {
    let received = false;
    http
      .get('/api/v1/x', { context: new HttpContext().set(SKIP_ERROR_MESSAGE, true) })
      .subscribe({ error: () => (received = true) });

    httpTesting
      .expectOne('/api/v1/x')
      .flush({ status: 400, title: 'Lỗi' }, { status: 400, statusText: 'Bad Request' });

    expect(messageError).not.toHaveBeenCalled();
    expect(received).toBe(true);
  });
});
