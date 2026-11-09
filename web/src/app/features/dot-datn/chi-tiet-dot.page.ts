import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  numberAttribute,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { NzBreadCrumbModule } from 'ng-zorro-antd/breadcrumb';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDescriptionsModule } from 'ng-zorro-antd/descriptions';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzTabsModule } from 'ng-zorro-antd/tabs';
import { NzTagModule } from 'ng-zorro-antd/tag';

import type { DotDatnItem } from '../../api/models/dot-datn-item';
import type { HoSoXetGiaoItem } from '../../api/models/ho-so-xet-giao-item';
import type { KhieuNaiItem } from '../../api/models/khieu-nai-item';
import { DotDatnService } from '../../api/services/dot-datn.service';
import { Quyen } from '../../core/auth/quyen';
import { CoQuyenDirective } from '../../shared/directives/co-quyen.directive';
import { nhanTrangThaiDot } from './dot-datn-nhan';
import { KetQuaXetGiaoDrawer } from './ket-qua-xet-giao.drawer';
import { TabKhieuNaiComponent } from './tab-khieu-nai.component';
import { TabQuyetDinhComponent } from './tab-quyet-dinh.component';
import { TabSinhVienComponent } from './tab-sinh-vien.component';

/** Chi tiết đợt: thông tin & mốc, tab Sinh viên (xét giao), Khiếu nại (A20), Quyết định giao (T03). */
@Component({
  selector: 'app-chi-tiet-dot-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    CoQuyenDirective,
    NzBreadCrumbModule,
    NzButtonModule,
    NzDescriptionsModule,
    NzPopconfirmModule,
    NzTabsModule,
    NzTagModule,
    KetQuaXetGiaoDrawer,
    TabKhieuNaiComponent,
    TabQuyetDinhComponent,
    TabSinhVienComponent,
  ],
  template: `
    <nz-breadcrumb>
      <nz-breadcrumb-item><a routerLink="/dot-datn">Đợt ĐATN</a></nz-breadcrumb-item>
      <nz-breadcrumb-item>{{ dot()?.maDot ?? '…' }}</nz-breadcrumb-item>
    </nz-breadcrumb>

    @if (dot(); as d) {
      <section class="page-head">
        <div>
          <h2>
            {{ d.tenDot }}
            <nz-tag [nzColor]="trangThai(d.trangThai).color">{{
              trangThai(d.trangThai).label
            }}</nz-tag>
          </h2>
        </div>
        @if (d.trangThai !== 'Dong') {
          <button
            nz-button
            nzDanger
            *coQuyen="quyen.DotQuanLy"
            nz-popconfirm
            nzPopconfirmTitle="Đóng đợt? Sau khi đóng không thêm sinh viên, không chạy xét."
            (nzOnConfirm)="dongDot()"
          >
            Đóng đợt
          </button>
        }
      </section>

      <nz-descriptions
        nzBordered
        nzSize="small"
        [nzColumn]="{ xxl: 4, xl: 4, lg: 3, md: 2, sm: 1, xs: 1 }"
      >
        <nz-descriptions-item nzTitle="Mã đợt">{{ d.maDot }}</nz-descriptions-item>
        <nz-descriptions-item nzTitle="Kỳ xét"
          >{{ d.namHoc }} – HK{{ d.hocKy }}</nz-descriptions-item
        >
        <nz-descriptions-item nzTitle="Bắt đầu">
          {{ d.ngayBatDau | date: 'dd/MM/yyyy' }} ({{ d.soTuan }} tuần)
        </nz-descriptions-item>
        <nz-descriptions-item nzTitle="Hạn nộp quyển">{{
          d.ngayNopQuyenHan | date: 'dd/MM/yyyy'
        }}</nz-descriptions-item>
        <nz-descriptions-item nzTitle="Số hồ sơ">{{ d.soHoSo }}</nz-descriptions-item>
      </nz-descriptions>

      <nz-tabs class="tabs">
        <nz-tab nzTitle="Sinh viên & xét giao">
          <app-tab-sinh-vien [dot]="d" (daThayDoi)="taiDot()" />
        </nz-tab>
        <nz-tab nzTitle="Khiếu nại kết quả xét" *coQuyen="quyen.XetDieuKienChay">
          <app-tab-khieu-nai
            [dot]="d"
            (xemKetQua)="xemTuKhieuNai($event)"
            (daXuLy)="tabSinhVien()?.taiLai()"
          />
        </nz-tab>
        <nz-tab nzTitle="Quyết định giao">
          <app-tab-quyet-dinh [dot]="d" (daGhiNhan)="tabSinhVien()?.taiLai()" />
        </nz-tab>
      </nz-tabs>

      <app-ket-qua-xet-giao-drawer [hoSo]="dangXem()" (dong)="dangXem.set(null)" />
    }
  `,
  styles: `
    nz-breadcrumb {
      margin-bottom: 12px;
    }
    h2 nz-tag {
      vertical-align: middle;
      margin-left: 8px;
    }
    .tabs {
      margin-top: 16px;
    }
  `,
})
export class ChiTietDotPage {
  private readonly api = inject(DotDatnService);
  private readonly message = inject(NzMessageService);
  private readonly destroyRef = inject(DestroyRef);

  /** :id của route (withComponentInputBinding). */
  readonly id = input.required({ transform: numberAttribute });

  protected readonly quyen = Quyen;
  protected readonly trangThai = nhanTrangThaiDot;
  protected readonly dot = signal<DotDatnItem | null>(null);
  protected readonly dangXem = signal<HoSoXetGiaoItem | null>(null);
  protected readonly tabSinhVien = viewChild(TabSinhVienComponent);

  constructor() {
    effect(() => {
      this.id();
      this.taiDot();
    });
  }

  protected taiDot(): void {
    this.api
      .dotDatnLay({ id: this.id() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((d) => this.dot.set(d));
  }

  protected dongDot(): void {
    this.api
      .dotDatnDong({ id: this.id() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((d) => {
        this.dot.set(d);
        this.message.success('Đã đóng đợt.');
      });
  }

  protected xemTuKhieuNai(k: KhieuNaiItem): void {
    this.api
      .dotDatnHoSo({ id: this.id(), q: k.maSV, pageSize: 50 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((res) => this.dangXem.set(res.items.find((x) => x.id === k.svTrongDotId) ?? null));
  }
}
