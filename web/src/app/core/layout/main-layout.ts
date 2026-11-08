import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink, RouterOutlet } from '@angular/router';
import { NzAvatarModule } from 'ng-zorro-antd/avatar';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';

import { CoSoResponse } from '../../api/models';
import { AuthService } from '../auth/auth.service';
import { taoMenu } from './menu';

/** Tên ngắn hiển thị của cơ sở: HN → UTC (Hà Nội), HCM → UTC2. */
export function tenNganCoSo(coSo: CoSoResponse | null): string {
  if (!coSo) {
    return '';
  }
  return coSo.maCoSo === 'HCM' ? 'UTC2' : coSo.maCoSo === 'HN' ? 'UTC (HN)' : coSo.maCoSo;
}

@Component({
  selector: 'app-main-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    RouterOutlet,
    RouterLink,
    NzAvatarModule,
    NzDropDownModule,
    NzIconModule,
    NzLayoutModule,
    NzMenuModule,
    NzSelectModule,
    NzTagModule,
    NzTooltipModule,
  ],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
})
export class MainLayout {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly routeChildren = inject(ActivatedRoute).routeConfig?.children;

  protected readonly thuGon = signal(false);
  protected readonly tenNganCoSo = tenNganCoSo;

  /** Menu tính lại khi quyền người dùng thay đổi (signal). */
  protected readonly menu = computed(() => {
    this.auth.quyen();
    return taoMenu(this.routeChildren, (...q) => this.auth.coQuyen(...q));
  });

  protected readonly tenHienThi = computed(() => {
    const nd = this.auth.nguoiDung();
    return nd?.hoTen || nd?.tenDangNhap || '';
  });

  protected chonCoSo(id: number): void {
    this.auth.chonCoSo(id);
    // Tải lại trang hiện tại để dữ liệu lọc theo cơ sở mới.
    const url = this.router.url;
    void this.router
      .navigateByUrl('/', { skipLocationChange: true })
      .then(() => this.router.navigateByUrl(url));
  }

  protected dangXuat(): void {
    this.auth.dangXuat().subscribe();
  }
}
