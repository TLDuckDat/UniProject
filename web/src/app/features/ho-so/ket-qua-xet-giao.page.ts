import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzTagModule } from 'ng-zorro-antd/tag';

import type { HoSoCuaToiXetGiaoItem } from '../../api/models/ho-so-cua-toi-xet-giao-item';
import { HoSoCuaToiService } from '../../api/services/ho-so-cua-toi.service';
import { SnapshotXetGiaoComponent } from '../../shared/components/snapshot-xet-giao/snapshot-xet-giao.component';
import { TrangThaiTagComponent } from '../../shared/components/trang-thai-tag/trang-thai-tag.component';
import { nhanTrangThaiDon } from '../dot-datn/dot-datn-nhan';

/** Sinh viên: "Kết quả xét giao ĐATN" của từng hồ sơ (đợt + CTĐT) và gửi khiếu nại (A20). */
@Component({
  selector: 'app-ket-qua-xet-giao-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    FormsModule,
    NzAlertModule,
    NzButtonModule,
    NzCardModule,
    NzEmptyModule,
    NzInputModule,
    NzModalModule,
    NzSpinModule,
    NzTagModule,
    SnapshotXetGiaoComponent,
    TrangThaiTagComponent,
  ],
  template: `
    <section class="page-head">
      <div>
        <h2>Kết quả xét giao ĐATN</h2>
        <p>
          Kết quả xét điều kiện nhận đồ án tốt nghiệp theo quy chế áp dụng cho khóa của bạn. Nếu
          thấy chưa đúng (ví dụ điểm đã được phúc khảo / cải thiện), bạn có thể gửi khiếu nại trước
          khi có quyết định giao.
        </p>
      </div>
    </section>

    <nz-spin [nzSpinning]="dangTai()">
      @for (h of hoSo(); track h.svTrongDotId) {
        <nz-card class="ho-so" [nzTitle]="tieuDe" [nzExtra]="hanhDong">
          <ng-template #tieuDe>
            {{ h.tenDot }}
            <span class="phu">({{ h.maDot }} · {{ h.maCTDT }} – {{ h.tenCTDT }})</span>
          </ng-template>
          <ng-template #hanhDong>
            <app-trang-thai-tag [trangThai]="h.trangThai" />
            @if (h.coTheKhieuNai) {
              <button nz-button nzSize="small" (click)="moKhieuNai(h)">Khiếu nại kết quả</button>
            }
          </ng-template>

          @if (h.daCoQuyetDinhGiao) {
            <nz-alert
              class="mb"
              nzType="success"
              nzShowIcon
              nzMessage="Đã có quyết định giao ĐATN."
            />
          }
          <app-snapshot-xet-giao [snapshots]="h.ketQuaMoiNhat ? [h.ketQuaMoiNhat] : []" />

          @if (h.khieuNai.length > 0) {
            <h4>Khiếu nại đã gửi</h4>
            <ul class="khieu-nai">
              @for (k of h.khieuNai; track k.id) {
                <li>
                  <nz-tag [nzColor]="trangThaiDon(k.trangThai).color">{{
                    trangThaiDon(k.trangThai).label
                  }}</nz-tag>
                  {{ k.ngayGui | date: 'dd/MM/yyyy HH:mm' }} – {{ k.noiDung }}
                  @if (k.ketQuaXuLy) {
                    <div class="phan-hoi"><strong>Phòng Đào tạo:</strong> {{ k.ketQuaXuLy }}</div>
                  }
                </li>
              }
            </ul>
          }
        </nz-card>
      } @empty {
        @if (!dangTai()) {
          <nz-empty nzNotFoundContent="Bạn chưa có hồ sơ trong đợt ĐATN nào." />
        }
      }
    </nz-spin>

    <nz-modal
      [nzVisible]="dangKhieuNai() !== null"
      nzTitle="Khiếu nại kết quả xét giao"
      nzOkText="Gửi khiếu nại"
      [nzOkDisabled]="!noiDung().trim()"
      [nzOkLoading]="dangGui()"
      (nzOnCancel)="dangKhieuNai.set(null)"
      (nzOnOk)="guiKhieuNai()"
    >
      <ng-container *nzModalContent>
        <p>
          Trình bày lý do (vd. học phần nào đã có điểm mới, phúc khảo…). Phòng Đào tạo sẽ xử lý và
          phản hồi.
        </p>
        <textarea
          nz-input
          rows="5"
          maxlength="4000"
          aria-label="Nội dung khiếu nại"
          [ngModel]="noiDung()"
          (ngModelChange)="noiDung.set($event)"
        ></textarea>
      </ng-container>
    </nz-modal>
  `,
  styles: `
    .ho-so {
      margin-bottom: 16px;
    }
    .phu {
      color: rgba(0, 0, 0, 0.55);
      font-weight: normal;
    }
    .mb {
      margin-bottom: 12px;
    }
    h4 {
      margin: 16px 0 8px;
    }
    .khieu-nai {
      padding-left: 18px;
    }
    .khieu-nai li {
      margin-bottom: 8px;
    }
    .phan-hoi {
      margin-top: 4px;
    }
  `,
})
export class KetQuaXetGiaoPage {
  private readonly api = inject(HoSoCuaToiService);
  private readonly message = inject(NzMessageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly trangThaiDon = nhanTrangThaiDon;
  protected readonly hoSo = signal<HoSoCuaToiXetGiaoItem[]>([]);
  protected readonly dangTai = signal(false);
  protected readonly dangKhieuNai = signal<HoSoCuaToiXetGiaoItem | null>(null);
  protected readonly noiDung = signal('');
  protected readonly dangGui = signal(false);

  constructor() {
    this.tai();
  }

  protected moKhieuNai(h: HoSoCuaToiXetGiaoItem): void {
    this.noiDung.set('');
    this.dangKhieuNai.set(h);
  }

  protected guiKhieuNai(): void {
    const h = this.dangKhieuNai();
    if (!h) {
      return;
    }
    this.dangGui.set(true);
    this.api
      .hoSoCuaToiKhieuNaiXetGiao({
        svTrongDotId: h.svTrongDotId,
        body: { noiDung: this.noiDung().trim() },
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.dangGui.set(false);
          this.dangKhieuNai.set(null);
          this.message.success('Đã gửi khiếu nại. Phòng Đào tạo sẽ xử lý và phản hồi.');
          this.tai();
        },
        error: () => this.dangGui.set(false),
      });
  }

  private tai(): void {
    this.dangTai.set(true);
    this.api
      .hoSoCuaToiXetGiao()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (rows) => {
          this.hoSo.set(rows);
          this.dangTai.set(false);
        },
        error: () => this.dangTai.set(false),
      });
  }
}
