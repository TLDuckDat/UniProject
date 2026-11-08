import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';

import { AuthService } from '../../core/auth/auth.service';

/** Form đăng nhập – Typed Reactive Form, mọi control non-nullable. */
type DangNhapForm = FormGroup<{
  tenDangNhap: FormControl<string>;
  matKhau: FormControl<string>;
}>;

@Component({
  selector: 'app-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    NzButtonModule,
    NzCardModule,
    NzFormModule,
    NzIconModule,
    NzInputModule,
  ],
  templateUrl: './login.page.html',
  styleUrl: './login.page.scss',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Query param ?returnUrl=… (withComponentInputBinding). */
  readonly returnUrl = input<string>();

  protected readonly dangGui = signal(false);
  protected readonly hienMatKhau = signal(false);

  protected readonly form: DangNhapForm = new FormGroup({
    tenDangNhap: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(50)],
    }),
    matKhau: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  protected dangNhap(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      Object.values(this.form.controls).forEach((c) =>
        c.updateValueAndValidity({ onlySelf: true }),
      );
      return;
    }

    const { tenDangNhap, matKhau } = this.form.getRawValue();
    this.dangGui.set(true);
    this.auth.dangNhap(tenDangNhap.trim(), matKhau).subscribe({
      next: () => void this.router.navigateByUrl(this.duongDanSau()),
      // Lỗi (sai mật khẩu, 429…) đã được interceptor hiển thị bằng nz-message.
      error: () => {
        this.dangGui.set(false);
        this.form.controls.matKhau.reset();
      },
    });
  }

  /** Chỉ cho quay lại đường dẫn nội bộ (chặn open redirect). */
  private duongDanSau(): string {
    const url = this.returnUrl();
    return url && url.startsWith('/') && !url.startsWith('//') ? url : '/';
  }
}
