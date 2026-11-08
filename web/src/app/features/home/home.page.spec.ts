import { registerLocaleData } from '@angular/common';
import localeVi from '@angular/common/locales/vi';
import { TestBed } from '@angular/core/testing';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { Observable, of, throwError } from 'rxjs';

import { PingResponse } from '../../api/models';
import { SystemService } from '../../api/services';
import { HomePage } from './home.page';

describe('HomePage', () => {
  beforeAll(() => registerLocaleData(localeVi));

  function setup(ping: () => Observable<PingResponse>) {
    TestBed.configureTestingModule({
      imports: [HomePage],
      providers: [provideNzIconsTesting(), { provide: SystemService, useValue: { ping } }],
    });
    return TestBed.createComponent(HomePage);
  }

  it('hiển thị kết quả ping: pong, version và thời gian máy chủ', async () => {
    const fixture = setup(() =>
      of({ message: 'pong', version: '0.1.0', serverTime: '2026-11-08T20:05:32+07:00' }),
    );
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('pong');
    expect(text).toContain('0.1.0');
    expect(text).toContain('08/11/2026 20:05:32');
  });

  it('hiển thị trạng thái lỗi khi API không trả lời', async () => {
    const fixture = setup(() => throwError(() => new Error('down')));
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Không gọi được API');
  });
});
