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
import { TabBmQuyenComponent } from './tab-bm-quyen.component';
import { TabGvQuyenComponent } from './tab-gv-quyen.component';
import { TabSvQuyenComponent } from './tab-sv-quyen.component';

/** UC-3b · Nộp quyển, ý kiến GVHD, đọc duyệt GVĐD, quyển cần đọc, GVĐD trễ hạn. */
@Component({
  selector: 'app-quyen-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    NzAlertModule,
    NzSelectModule,
    NzTabsModule,
    TabSvQuyenComponent,
    TabGvQuyenComponent,
    TabBmQuyenComponent,
  ],
  template: `
    <section class="page-head">
      <div>
        <h2>Nộp & đọc quyển</h2>
        <p>
          Nộp quyển, ý kiến hướng dẫn, đọc duyệt của giảng viên phản biện. Quá hạn đọc được Bộ môn
          xử lý.
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
          @if (laSv()) {
            <nz-tab nzTitle="Quyển của tôi">
              <app-tab-sv-quyen [dotId]="id" />
            </nz-tab>
          }
          @if (laGv()) {
            <nz-tab nzTitle="Hướng dẫn & đọc duyệt">
              <app-tab-gv-quyen [dotId]="id" />
            </nz-tab>
          }
          @if (laBm()) {
            <nz-tab nzTitle="Bộ môn – GVĐD trễ hạn">
              <app-tab-bm-quyen [dotId]="id" />
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
export class QuyenPage implements OnInit {
  private readonly api = inject(DeTaiService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly dots = signal<DotTomTat[]>([]);
  protected readonly dotId = signal<number | null>(null);
  protected readonly laSv = computed(() => this.auth.nguoiDung()?.sinhVienId != null);
  protected readonly laGv = computed(() => this.auth.nguoiDung()?.giangVienId != null);
  protected readonly laBm = computed(() => this.auth.coQuyen(Quyen.PhanCongQuanLy));

  ngOnInit(): void {
    this.api
      .deTaiDanhSachDot()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) => {
        this.dots.set(ds);
        this.dotId.set((ds.find((d) => d.trangThai !== 'Dong') ?? ds[0])?.id ?? null);
      });
  }
}
