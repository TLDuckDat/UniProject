import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { of } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { LoginPage } from './login.page';

describe('LoginPage', () => {
  const dangNhap = vi.fn();
  let fixture: ComponentFixture<LoginPage>;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(async () => {
    dangNhap.mockReset().mockReturnValue(of(undefined));
    TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        provideRouter([]),
        provideNzIconsTesting(),
        { provide: AuthService, useValue: { dangNhap } },
      ],
    });
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(LoginPage);
    await fixture.whenStable();
  });

  function nhap(ten: string, matKhau: string): void {
    const el = fixture.nativeElement as HTMLElement;
    const input = (id: string, v: string) => {
      const i = el.querySelector<HTMLInputElement>(`#${id}`)!;
      i.value = v;
      i.dispatchEvent(new Event('input'));
    };
    input('tenDangNhap', ten);
    input('matKhau', matKhau);
  }

  const submit = async () => {
    (fixture.nativeElement as HTMLElement)
      .querySelector('form')!
      .dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  };

  it('form trống: không gọi đăng nhập', async () => {
    await submit();

    expect(dangNhap).not.toHaveBeenCalled();
  });

  it('hợp lệ: đăng nhập rồi chuyển tới returnUrl nội bộ', async () => {
    fixture.componentRef.setInput('returnUrl', '/de-tai');
    nhap('  pdt.hn ', 'Upm@123456');

    await submit();

    expect(dangNhap).toHaveBeenCalledWith('pdt.hn', 'Upm@123456');
    expect(navigate).toHaveBeenCalledWith('/de-tai');
  });

  it('chặn open redirect: returnUrl ra ngoài site → về trang chủ', async () => {
    fixture.componentRef.setInput('returnUrl', '//evil.example.com');
    nhap('pdt.hn', 'x');

    await submit();

    expect(navigate).toHaveBeenCalledWith('/');
  });
});
