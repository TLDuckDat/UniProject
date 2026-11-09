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
import { TabBmTienDoComponent } from './tab-bm-tien-do.component';
import { TabGvTienDoComponent } from './tab-gv-tien-do.component';
import { TabSvTienDoComponent } from './tab-sv-tien-do.component';

/** UC-3a · Tiến độ 3 mốc: chọn đợt, rồi tab theo vai trò (SV nộp báo cáo, GVHD nhận xét, Bộ môn theo dõi). */
@Component({
  selector: 'app-tien-do-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    NzAlertModule,
    NzSelectModule,
    NzTabsModule,
    TabSvTienDoComponent,
    TabGvTienDoComponent,
    TabBmTienDoComponent,
  ],
  template: `
    <section class="page-head">
      <div>
        <h2>Tiến độ ĐATN (3 mốc)</h2>
        <p>Mốc tuần 3–4 (đề cương), tuần 8–9 (60–70%), tuần 12–13 (duyệt 100% và nộp quyển).</p>
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
            <nz-tab nzTitle="Tiến độ của tôi">
              <app-tab-sv-tien-do [dotId]="id" />
            </nz-tab>
          }
          @if (laGv()) {
            <nz-tab nzTitle="Hướng dẫn – nhận xét">
              <app-tab-gv-tien-do [dotId]="id" />
            </nz-tab>
          }
          @if (laBm()) {
            <nz-tab nzTitle="Bộ môn – theo dõi">
              <app-tab-bm-tien-do [dotId]="id" />
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
export class TienDoPage implements OnInit {
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
