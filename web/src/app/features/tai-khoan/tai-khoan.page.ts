import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDescriptionsModule } from 'ng-zorro-antd/descriptions';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';

import { AuthService } from '../../core/auth/auth.service';

/** Thông tin tài khoản đang đăng nhập: vai trò + phạm vi + quyền (dữ liệu từ /api/v1/auth/me). */
@Component({
  selector: 'app-tai-khoan-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NzCardModule, NzDescriptionsModule, NzTableModule, NzTagModule],
  template: `
    @if (auth.nguoiDung(); as nd) {
      <nz-card nzTitle="Tài khoản của tôi">
        <nz-descriptions nzBordered [nzColumn]="1">
          <nz-descriptions-item nzTitle="Tên đăng nhập">{{ nd.tenDangNhap }}</nz-descriptions-item>
          <nz-descriptions-item nzTitle="Họ tên">{{ nd.hoTen || '—' }}</nz-descriptions-item>
          <nz-descriptions-item nzTitle="Email">{{ nd.email || '—' }}</nz-descriptions-item>
        </nz-descriptions>
      </nz-card>

      <nz-card nzTitle="Vai trò và phạm vi dữ liệu" class="mt">
        <nz-table [nzData]="nd.vaiTro" [nzShowPagination]="false" nzSize="small" #bang>
          <thead>
            <tr>
              <th>Vai trò</th>
              <th>Cơ sở</th>
              <th>Khoa</th>
              <th>Bộ môn</th>
            </tr>
          </thead>
          <tbody>
            @for (v of bang.data; track $index) {
              <tr>
                <td>{{ v.tenVaiTro }}</td>
                <td>{{ v.maCoSo ?? 'Toàn hệ thống' }}</td>
                <td>{{ v.tenKhoa ?? (v.maCoSo ? 'Mọi khoa' : '—') }}</td>
                <td>{{ v.tenBoMon ?? (v.tenKhoa ? 'Mọi bộ môn' : '—') }}</td>
              </tr>
            }
          </tbody>
        </nz-table>
      </nz-card>

      <nz-card nzTitle="Quyền chức năng" class="mt">
        @for (q of nd.quyen; track q) {
          <nz-tag>{{ q }}</nz-tag>
        } @empty {
          <span>Không có quyền chức năng nào.</span>
        }
      </nz-card>
    }
  `,
  styles: `
    .mt {
      margin-top: 16px;
    }
  `,
})
export class TaiKhoanPage {
  protected readonly auth = inject(AuthService);
}
