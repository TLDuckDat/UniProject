import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTabsModule } from 'ng-zorro-antd/tabs';

import type { DotTomTat } from '../../api/models/dot-tom-tat';
import { DeTaiService } from '../../api/services/de-tai.service';
import { AuthService } from '../../core/auth/auth.service';
import { Quyen } from '../../core/auth/quyen';
import { TabBmComponent } from './tab-bm.component';
import { TabGvComponent } from './tab-gv.component';
import { TabSvComponent } from './tab-sv.component';

/**
 * UC-2 · Đề tài & phân công. Chọn đợt rồi chia tab theo vai trò:
 * GV (đề tài của tôi, duyệt SV), SV (danh mục, nguyện vọng, tự đề xuất, đơn điều chỉnh), Trưởng BM (duyệt, phân công, đơn).
 */
@Component({
  selector: 'app-de-tai-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    NzAlertModule,
    NzSelectModule,
    NzTabsModule,
    TabGvComponent,
    TabSvComponent,
    TabBmComponent,
  ],
  template: `
    <section class="page-head">
      <div>
        <h2>Đề tài & phân công</h2>
        <p>
          Đề xuất đề tài, đăng ký nguyện vọng, duyệt và phân công giảng viên hướng dẫn cho từng sinh
          viên.
        </p>
      </div>
    </section>

    @if (dots().length === 0) {
      <nz-alert nzType="info" nzShowIcon nzMessage="Chưa có đợt ĐATN nào cho tài khoản này." />
    } @else {
      <div class="chon-dot">
        <span>Đợt:</span>
        <nz-select
          class="chon"
          [ngModel]="dotId()"
          (ngModelChange)="dotId.set($event)"
          aria-label="Chọn đợt"
        >
          @for (d of dots(); track d.id) {
            <nz-option
              [nzValue]="d.id"
              [nzLabel]="d.maDot + ' – ' + d.tenDot + ' (' + d.namHoc + ')'"
            />
          }
        </nz-select>
      </div>

      @if (dotId(); as id) {
        <nz-tabs>
          @if (laGv()) {
            <nz-tab nzTitle="Đề tài của tôi">
              <app-tab-gv [dotId]="id" />
            </nz-tab>
          }
          @if (laSv()) {
            <nz-tab nzTitle="Đề tài & nguyện vọng">
              <app-tab-sv [dotId]="id" />
            </nz-tab>
          }
          @if (laBm()) {
            <nz-tab nzTitle="Duyệt & phân công">
              <app-tab-bm [dotId]="id" />
            </nz-tab>
          }
        </nz-tabs>
      }
    }
  `,
  styles: `
    .chon-dot {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 16px;
    }
    .chon {
      min-width: 420px;
      max-width: 100%;
    }
  `,
})
export class DeTaiPage implements OnInit {
  private readonly api = inject(DeTaiService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly dots = signal<DotTomTat[]>([]);
  protected readonly dotId = signal<number | null>(null);
  protected readonly laGv = computed(() => this.auth.nguoiDung()?.giangVienId != null);
  protected readonly laSv = computed(() => this.auth.nguoiDung()?.sinhVienId != null);
  protected readonly laBm = computed(() => this.auth.coQuyen(Quyen.PhanCongQuanLy));

  ngOnInit(): void {
    this.api
      .deTaiDanhSachDot()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) => {
        this.dots.set(ds);
        const dangMo = ds.find((d) => d.trangThai !== 'Dong') ?? ds[0];
        this.dotId.set(dangMo?.id ?? null);
      });
  }
}
