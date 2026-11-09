import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormControl,
  FormGroup,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { forkJoin } from 'rxjs';

import type { DotDatnItem } from '../../api/models/dot-datn-item';
import type { HoSoXetGiaoItem } from '../../api/models/ho-so-xet-giao-item';
import type { QuyetDinhGiaoItem } from '../../api/models/quyet-dinh-giao-item';
import { DotDatnService } from '../../api/services/dot-datn.service';
import { AuthService } from '../../core/auth/auth.service';
import { Quyen } from '../../core/auth/quyen';

interface QuyetDinhControls {
  soQuyetDinh: FormControl<string>;
  ngayKy: FormControl<string>;
  nguoiKy: FormControl<string>;
}

/**
 * Ghi nhận Quyết định giao ĐATN (T03): chỉ hồ sơ đủ điều kiện ĐÃ có đề tài; sau khi ghi nhận dữ liệu bị khóa
 * (không xét lại / khiếu nại kết quả giao).
 */
@Component({
  selector: 'app-tab-quyet-dinh',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    NzAlertModule,
    NzButtonModule,
    NzFormModule,
    NzInputModule,
    NzTableModule,
    NzTagModule,
    NzTooltipModule,
  ],
  template: `
    <h4>Quyết định đã ghi nhận</h4>
    <nz-table
      #qdBang
      nzSize="small"
      [nzData]="quyetDinh()"
      [nzShowPagination]="false"
      [nzLoading]="dangTai()"
    >
      <thead>
        <tr>
          <th>Số quyết định</th>
          <th>Ngày ký</th>
          <th>Người ký</th>
          <th nzAlign="right">Số hồ sơ</th>
          <th>Dữ liệu</th>
        </tr>
      </thead>
      <tbody>
        @for (q of qdBang.data; track q.id) {
          <tr>
            <td>{{ q.soQuyetDinh }}</td>
            <td>{{ q.ngayKy | date: 'dd/MM/yyyy' }}</td>
            <td>{{ q.nguoiKy }}</td>
            <td class="so">{{ q.svTrongDotIds.length }}</td>
            <td>
              @if (q.daKhoaDuLieu) {
                <nz-tag>Đã khóa</nz-tag>
              }
            </td>
          </tr>
        }
      </tbody>
    </nz-table>

    @if (coQuyen() && dot().trangThai !== 'Dong') {
      <h4>Ghi nhận quyết định giao mới</h4>
      <nz-alert
        nzType="info"
        nzShowIcon
        nzMessage="Chỉ chọn được hồ sơ đủ điều kiện (kết quả xét gần nhất) và đã có đề tài được duyệt. Đề tài được gắn ở module Đề tài & phân công."
      />
      <form nz-form nzLayout="vertical" class="form" [formGroup]="form" (ngSubmit)="ghiNhan()">
        <nz-form-item>
          <nz-form-label nzRequired>Số quyết định</nz-form-label>
          <nz-form-control nzErrorTip="Bắt buộc, tối đa 50 ký tự">
            <input nz-input formControlName="soQuyetDinh" maxlength="50" />
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label nzRequired>Ngày ký</nz-form-label>
          <nz-form-control>
            <input nz-input type="date" formControlName="ngayKy" />
          </nz-form-control>
        </nz-form-item>
        <nz-form-item>
          <nz-form-label>Người ký</nz-form-label>
          <nz-form-control>
            <input nz-input formControlName="nguoiKy" maxlength="100" />
          </nz-form-control>
        </nz-form-item>
        <p class="goi-y">File scan quyết định: chưa hỗ trợ tải lên (chờ dịch vụ lưu file).</p>
      </form>

      <nz-table
        #hsBang
        nzSize="small"
        [nzData]="ungVien()"
        [nzPageSize]="50"
        [nzLoading]="dangTai()"
      >
        <thead>
          <tr>
            <th
              [nzChecked]="chonTatCa()"
              (nzCheckedChange)="chonHet($event)"
              aria-label="Chọn tất cả"
            ></th>
            <th>Mã SV</th>
            <th>Họ tên</th>
            <th>CTĐT</th>
            <th>Đề tài</th>
          </tr>
        </thead>
        <tbody>
          @for (h of hsBang.data; track h.id) {
            <tr>
              <td
                [nzDisabled]="!h.coDeTai"
                [nzChecked]="daChon().has(h.id)"
                (nzCheckedChange)="chon(h.id, $event)"
              ></td>
              <td>{{ h.maSV }}</td>
              <td>{{ h.hoTen }}</td>
              <td>{{ h.maCTDT }}</td>
              <td>
                @if (h.coDeTai) {
                  <nz-tag nzColor="green">Đã có</nz-tag>
                } @else {
                  <nz-tag nz-tooltip nzTooltipTitle="Chưa thể gắn quyết định giao"
                    >Chưa có đề tài</nz-tag
                  >
                }
              </td>
            </tr>
          }
        </tbody>
      </nz-table>
      <button
        nz-button
        nzType="primary"
        class="nut"
        [disabled]="form.invalid || daChon().size === 0"
        [nzLoading]="dangLuu()"
        (click)="ghiNhan()"
      >
        Ghi nhận quyết định cho {{ daChon().size }} hồ sơ
      </button>
    }
  `,
  styles: `
    h4 {
      margin: 16px 0 8px;
    }
    .so {
      text-align: right;
    }
    .form {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 0 16px;
      margin-top: 12px;
      max-width: 760px;
    }
    .goi-y {
      grid-column: 1 / -1;
      color: rgba(0, 0, 0, 0.55);
      font-size: 13px;
    }
    .nut {
      margin-top: 12px;
    }
  `,
})
export class TabQuyetDinhComponent implements OnInit {
  private readonly api = inject(DotDatnService);
  private readonly message = inject(NzMessageService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(AuthService);

  readonly dot = input.required<DotDatnItem>();
  readonly daGhiNhan = output<void>();

  protected readonly coQuyen = computed(() => this.auth.coQuyen(Quyen.QuyetDinhGhiNhan));
  protected readonly quyetDinh = signal<QuyetDinhGiaoItem[]>([]);
  protected readonly ungVien = signal<HoSoXetGiaoItem[]>([]);
  protected readonly daChon = signal<ReadonlySet<number>>(new Set());
  protected readonly chonTatCa = computed(() => {
    const chon = this.ungVien().filter((h) => h.coDeTai);
    return chon.length > 0 && chon.every((h) => this.daChon().has(h.id));
  });
  protected readonly dangTai = signal(false);
  protected readonly dangLuu = signal(false);

  protected readonly form: FormGroup<QuyetDinhControls> = inject(NonNullableFormBuilder).group({
    soQuyetDinh: ['', [Validators.required, Validators.maxLength(50)]],
    ngayKy: ['', Validators.required],
    nguoiKy: ['', Validators.maxLength(100)],
  });

  ngOnInit(): void {
    this.tai();
  }

  protected chon(id: number, checked: boolean): void {
    const next = new Set(this.daChon());
    if (checked) {
      next.add(id);
    } else {
      next.delete(id);
    }
    this.daChon.set(next);
  }

  protected chonHet(checked: boolean): void {
    this.daChon.set(
      new Set(
        checked
          ? this.ungVien()
              .filter((h) => h.coDeTai)
              .map((h) => h.id)
          : [],
      ),
    );
  }

  protected ghiNhan(): void {
    if (this.form.invalid || this.daChon().size === 0) {
      return;
    }
    const v = this.form.getRawValue();
    this.dangLuu.set(true);
    this.api
      .dotDatnGhiNhanQuyetDinhGiao({
        id: this.dot().id,
        body: {
          soQuyetDinh: v.soQuyetDinh.trim(),
          ngayKy: v.ngayKy,
          nguoiKy: v.nguoiKy.trim() || null,
          fileScanId: null,
          svTrongDotIds: [...this.daChon()],
        },
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (qd) => {
          this.dangLuu.set(false);
          this.message.success(
            `Đã ghi nhận quyết định ${qd.soQuyetDinh} cho ${qd.svTrongDotIds.length} hồ sơ.`,
          );
          this.form.reset();
          this.daChon.set(new Set());
          this.tai();
          this.daGhiNhan.emit();
        },
        error: () => this.dangLuu.set(false),
      });
  }

  private tai(): void {
    this.dangTai.set(true);
    forkJoin({
      quyetDinh: this.api.dotDatnQuyetDinhGiao({ id: this.dot().id }),
      hoSo: this.api.dotDatnHoSo({
        id: this.dot().id,
        trangThai: 'CHO_XET',
        ketQuaXet: 'DU_DK',
        pageSize: 200,
      }),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ quyetDinh, hoSo }) => {
          this.quyetDinh.set(quyetDinh);
          this.ungVien.set(hoSo.items.filter((h) => !h.soQuyetDinhGiao));
          this.dangTai.set(false);
        },
        error: () => this.dangTai.set(false),
      });
  }
}
