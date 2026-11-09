import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzRadioModule } from 'ng-zorro-antd/radio';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';

import type { DotDatnItem } from '../../api/models/dot-datn-item';
import type { KhieuNaiItem } from '../../api/models/khieu-nai-item';
import type { TrangThaiDonTu } from '../../api/models/trang-thai-don-tu';
import { DonTuService } from '../../api/services/don-tu.service';
import { DotDatnService } from '../../api/services/dot-datn.service';
import { nhanTrangThaiDon } from './dot-datn-nhan';

/** A20 · Phòng ĐT xử lý khiếu nại kết quả xét giao: chấp nhận → xét lại (A19); từ chối → bắt buộc lý do. */
@Component({
  selector: 'app-tab-khieu-nai',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    FormsModule,
    NzButtonModule,
    NzInputModule,
    NzModalModule,
    NzPopconfirmModule,
    NzRadioModule,
    NzTableModule,
    NzTagModule,
  ],
  template: `
    <nz-radio-group
      class="loc"
      [ngModel]="trangThai()"
      (ngModelChange)="trangThai.set($event); tai()"
      aria-label="Lọc khiếu nại"
    >
      <!-- eslint-disable-next-line @angular-eslint/template/label-has-associated-control -- NG-ZORRO dựng <input> bên trong label -->
      <label nz-radio-button nzValue="ChoXuLy">Chờ xử lý</label>
      <!-- eslint-disable-next-line @angular-eslint/template/label-has-associated-control -- NG-ZORRO dựng <input> bên trong label -->
      <label nz-radio-button nzValue="DaDuyet">Đã chấp nhận</label>
      <!-- eslint-disable-next-line @angular-eslint/template/label-has-associated-control -- NG-ZORRO dựng <input> bên trong label -->
      <label nz-radio-button nzValue="TuChoi">Từ chối</label>
      <!-- eslint-disable-next-line @angular-eslint/template/label-has-associated-control -- NG-ZORRO dựng <input> bên trong label -->
      <label nz-radio-button [nzValue]="null">Tất cả</label>
    </nz-radio-group>

    <nz-table #bang nzSize="middle" [nzData]="items()" [nzLoading]="dangTai()" [nzPageSize]="20">
      <thead>
        <tr>
          <th>Mã SV</th>
          <th>Họ tên</th>
          <th>Nội dung</th>
          <th>Ngày gửi</th>
          <th>Trạng thái</th>
          <th>Kết quả xử lý</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        @for (k of bang.data; track k.id) {
          <tr>
            <td>{{ k.maSV }}</td>
            <td>{{ k.hoTen }}</td>
            <td class="noi-dung">{{ k.noiDung }}</td>
            <td>{{ k.ngayGui | date: 'dd/MM/yyyy HH:mm' }}</td>
            <td>
              <nz-tag [nzColor]="nhan(k.trangThai).color">{{ nhan(k.trangThai).label }}</nz-tag>
            </td>
            <td>{{ k.ketQuaXuLy }}</td>
            <td class="hanh-dong">
              <button nz-button nzType="link" type="button" (click)="xemKetQua.emit(k)">
                Kết quả xét
              </button>
              @if (k.trangThai === 'ChoXuLy') {
                <button
                  nz-button
                  nzType="link"
                  type="button"
                  nz-popconfirm
                  nzPopconfirmTitle="Chấp nhận và xét lại hồ sơ với dữ liệu hiện tại?"
                  (nzOnConfirm)="xuLy(k, true, null)"
                >
                  Chấp nhận
                </button>
                <button nz-button nzType="link" type="button" class="tu-choi" (click)="moTuChoi(k)">
                  Từ chối
                </button>
              }
            </td>
          </tr>
        }
      </tbody>
    </nz-table>

    <nz-modal
      [nzVisible]="tuChoi() !== null"
      nzTitle="Từ chối khiếu nại"
      nzOkText="Từ chối"
      nzOkDanger
      [nzOkDisabled]="!lyDo().trim()"
      [nzOkLoading]="dangXuLy()"
      (nzOnCancel)="tuChoi.set(null)"
      (nzOnOk)="xuLy(tuChoi()!, false, lyDo())"
    >
      <ng-container *nzModalContent>
        <p>Lý do từ chối (bắt buộc, sinh viên sẽ thấy nội dung này):</p>
        <textarea
          nz-input
          rows="4"
          maxlength="900"
          aria-label="Lý do từ chối"
          [ngModel]="lyDo()"
          (ngModelChange)="lyDo.set($event)"
        ></textarea>
      </ng-container>
    </nz-modal>
  `,
  styles: `
    .loc {
      margin-bottom: 12px;
    }
    .noi-dung {
      max-width: 360px;
      white-space: pre-line;
    }
    .hanh-dong button {
      padding: 0 6px;
    }
    .tu-choi {
      color: #cf1322 !important;
    }
  `,
})
export class TabKhieuNaiComponent implements OnInit {
  private readonly api = inject(DotDatnService);
  private readonly donTuApi = inject(DonTuService);
  private readonly message = inject(NzMessageService);
  private readonly destroyRef = inject(DestroyRef);

  readonly dot = input.required<DotDatnItem>();
  readonly xemKetQua = output<KhieuNaiItem>();
  /** Đã xử lý một đơn (chấp nhận có thể đổi trạng thái hồ sơ) → tab SV cần tải lại. */
  readonly daXuLy = output<void>();

  protected readonly nhan = nhanTrangThaiDon;
  protected readonly items = signal<KhieuNaiItem[]>([]);
  protected readonly trangThai = signal<TrangThaiDonTu | null>('ChoXuLy');
  protected readonly dangTai = signal(false);
  protected readonly tuChoi = signal<KhieuNaiItem | null>(null);
  protected readonly lyDo = signal('');
  protected readonly dangXuLy = signal(false);

  ngOnInit(): void {
    this.tai();
  }

  protected tai(): void {
    this.dangTai.set(true);
    this.api
      .dotDatnKhieuNai({ id: this.dot().id, trangThai: this.trangThai() ?? undefined })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (rows) => {
          this.items.set(rows);
          this.dangTai.set(false);
        },
        error: () => this.dangTai.set(false),
      });
  }

  protected moTuChoi(k: KhieuNaiItem): void {
    this.lyDo.set('');
    this.tuChoi.set(k);
  }

  protected xuLy(k: KhieuNaiItem, chapNhan: boolean, lyDo: string | null): void {
    this.dangXuLy.set(true);
    this.donTuApi
      .donTuXuLyKhieuNaiXetGiao({ id: k.id, body: { chapNhan, lyDo: lyDo?.trim() || null } })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.dangXuLy.set(false);
          this.tuChoi.set(null);
          this.message.success(
            chapNhan ? 'Đã chấp nhận và xét lại hồ sơ.' : 'Đã từ chối khiếu nại.',
          );
          this.tai();
          this.daXuLy.emit();
        },
        error: () => this.dangXuLy.set(false),
      });
  }
}
