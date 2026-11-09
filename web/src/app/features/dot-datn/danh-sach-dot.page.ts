import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormControl,
  FormGroup,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTableModule, NzTableQueryParams } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';

import type { DotDatnItem } from '../../api/models/dot-datn-item';
import type { DotDatnRequest } from '../../api/models/dot-datn-request';
import { DotDatnService } from '../../api/services/dot-datn.service';
import { AuthService } from '../../core/auth/auth.service';
import { Quyen } from '../../core/auth/quyen';
import { CoQuyenDirective } from '../../shared/directives/co-quyen.directive';
import { nhanTrangThaiDot } from './dot-datn-nhan';

interface DotControls {
  maDot: FormControl<string>;
  tenDot: FormControl<string>;
  namHoc: FormControl<string>;
  hocKy: FormControl<number>;
  ngayBatDau: FormControl<string>;
  soTuan: FormControl<number>;
  ngayNopQuyenHan: FormControl<string>;
}

/** UC-1 · Phòng ĐT: danh sách đợt ĐATN của cơ sở, mở đợt mới và cấu hình mốc thời gian. */
@Component({
  selector: 'app-danh-sach-dot-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    CoQuyenDirective,
    NzButtonModule,
    NzDrawerModule,
    NzFormModule,
    NzIconModule,
    NzInputModule,
    NzSelectModule,
    NzTableModule,
    NzTagModule,
  ],
  template: `
    <section class="page-head">
      <div>
        <h2>Đợt ĐATN & xét điều kiện giao</h2>
        <p>Mở đợt, thêm sinh viên, chạy xét điều kiện giao và ghi nhận quyết định giao ĐATN.</p>
      </div>
      <button nz-button nzType="primary" *coQuyen="quyen.DotQuanLy" (click)="moForm(null)">
        <nz-icon nzType="plus" /> Mở đợt mới
      </button>
    </section>

    <input
      nz-input
      class="tim"
      placeholder="Tìm theo mã, tên đợt, năm học"
      aria-label="Tìm đợt"
      [value]="q()"
      (input)="tim($any($event.target).value)"
    />
    <nz-table
      #bang
      nzShowSizeChanger
      [nzData]="items()"
      [nzFrontPagination]="false"
      [nzTotal]="total()"
      [nzPageIndex]="page()"
      [nzPageSize]="pageSize()"
      [nzLoading]="dangTai()"
      (nzQueryParams)="doiTrang($event)"
    >
      <thead>
        <tr>
          <th>Mã đợt</th>
          <th>Tên đợt</th>
          <th>Năm học / HK</th>
          <th>Bắt đầu</th>
          <th>Hạn nộp quyển</th>
          <th nzAlign="right">Số hồ sơ</th>
          <th>Trạng thái</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        @for (d of bang.data; track d.id) {
          <tr>
            <td>
              <a [routerLink]="[d.id]">{{ d.maDot }}</a>
            </td>
            <td>{{ d.tenDot }}</td>
            <td>{{ d.namHoc }} / HK{{ d.hocKy }}</td>
            <td>{{ d.ngayBatDau | date: 'dd/MM/yyyy' }} ({{ d.soTuan }} tuần)</td>
            <td>{{ d.ngayNopQuyenHan | date: 'dd/MM/yyyy' }}</td>
            <td class="so">{{ d.soHoSo }}</td>
            <td>
              <nz-tag [nzColor]="trangThai(d.trangThai).color">{{
                trangThai(d.trangThai).label
              }}</nz-tag>
            </td>
            <td>
              <a [routerLink]="[d.id]">Chi tiết</a>
              @if (d.trangThai !== 'Dong') {
                <button
                  nz-button
                  nzType="link"
                  type="button"
                  class="ml"
                  *coQuyen="quyen.DotQuanLy"
                  (click)="moForm(d)"
                >
                  Sửa
                </button>
              }
            </td>
          </tr>
        }
      </tbody>
    </nz-table>

    <nz-drawer
      [nzVisible]="formMo()"
      [nzWidth]="480"
      [nzTitle]="dangSua() ? 'Sửa đợt ' + dangSua()!.maDot : 'Mở đợt ĐATN mới'"
      (nzOnClose)="formMo.set(false)"
    >
      <ng-container *nzDrawerContent>
        <form nz-form nzLayout="vertical" [formGroup]="form" (ngSubmit)="luu()">
          <p>
            Cơ sở: <strong>{{ auth.coSoDangLamViec()?.tenCoSo ?? '—' }}</strong>
          </p>
          <nz-form-item>
            <nz-form-label nzRequired>Mã đợt</nz-form-label>
            <nz-form-control nzErrorTip="Tối đa 20 ký tự">
              <input nz-input formControlName="maDot" maxlength="20" />
            </nz-form-control>
          </nz-form-item>
          <nz-form-item>
            <nz-form-label nzRequired>Tên đợt</nz-form-label>
            <nz-form-control nzErrorTip="Tối đa 150 ký tự">
              <input nz-input formControlName="tenDot" maxlength="150" />
            </nz-form-control>
          </nz-form-item>
          <div class="hang">
            <nz-form-item>
              <nz-form-label nzRequired>Năm học</nz-form-label>
              <nz-form-control nzErrorTip="Dạng 2026-2027">
                <input nz-input formControlName="namHoc" placeholder="2026-2027" />
              </nz-form-control>
            </nz-form-item>
            <nz-form-item>
              <nz-form-label nzRequired>Học kỳ</nz-form-label>
              <nz-form-control>
                <nz-select formControlName="hocKy">
                  <nz-option [nzValue]="1" nzLabel="Học kỳ 1" />
                  <nz-option [nzValue]="2" nzLabel="Học kỳ 2" />
                  <nz-option [nzValue]="3" nzLabel="Học kỳ hè" />
                </nz-select>
              </nz-form-control>
            </nz-form-item>
          </div>
          <div class="hang">
            <nz-form-item>
              <nz-form-label nzRequired>Ngày bắt đầu</nz-form-label>
              <nz-form-control>
                <input nz-input type="date" formControlName="ngayBatDau" />
              </nz-form-control>
            </nz-form-item>
            <nz-form-item>
              <nz-form-label nzRequired>Số tuần</nz-form-label>
              <nz-form-control>
                <nz-select formControlName="soTuan">
                  <nz-option [nzValue]="12" nzLabel="12 tuần" />
                  <nz-option [nzValue]="13" nzLabel="13 tuần" />
                </nz-select>
              </nz-form-control>
            </nz-form-item>
          </div>
          <nz-form-item>
            <nz-form-label nzRequired>Hạn nộp quyển</nz-form-label>
            <nz-form-control>
              <input nz-input type="date" formControlName="ngayNopQuyenHan" />
            </nz-form-control>
          </nz-form-item>
          <p class="goi-y">
            Năm học + học kỳ là "kỳ xét" dùng để xác định sinh viên đúng / trễ tiến độ (A06, A07);
            không đổi được sau khi đã chạy xét.
          </p>
          <button nz-button nzType="primary" [nzLoading]="dangLuu()" [disabled]="form.invalid">
            Lưu
          </button>
        </form>
      </ng-container>
    </nz-drawer>
  `,
  styles: `
    .tim {
      max-width: 360px;
      margin-bottom: 12px;
    }
    .so {
      text-align: right;
    }
    .ml {
      margin-left: 12px;
    }
    .hang {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
    }
    .goi-y {
      color: rgba(0, 0, 0, 0.55);
      font-size: 13px;
    }
  `,
})
export class DanhSachDotPage {
  private readonly api = inject(DotDatnService);
  private readonly message = inject(NzMessageService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly auth = inject(AuthService);
  protected readonly quyen = Quyen;
  protected readonly trangThai = nhanTrangThaiDot;

  protected readonly items = signal<DotDatnItem[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(20);
  protected readonly q = signal('');
  protected readonly dangTai = signal(false);
  protected readonly formMo = signal(false);
  protected readonly dangSua = signal<DotDatnItem | null>(null);
  protected readonly dangLuu = signal(false);

  protected readonly form: FormGroup<DotControls> = inject(NonNullableFormBuilder).group({
    maDot: ['', [Validators.required, Validators.maxLength(20)]],
    tenDot: ['', [Validators.required, Validators.maxLength(150)]],
    namHoc: ['', [Validators.required, Validators.pattern(/^\d{4}-\d{4}$/)]],
    hocKy: [1, Validators.required],
    ngayBatDau: ['', Validators.required],
    soTuan: [12, Validators.required],
    ngayNopQuyenHan: ['', Validators.required],
  });

  private timer: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    this.tai();
  }

  protected tim(value: string): void {
    this.q.set(value);
    clearTimeout(this.timer);
    this.timer = setTimeout(() => {
      this.page.set(1);
      this.tai();
    }, 300);
  }

  protected doiTrang(params: NzTableQueryParams): void {
    if (params.pageIndex === this.page() && params.pageSize === this.pageSize()) {
      return;
    }
    this.page.set(params.pageIndex);
    this.pageSize.set(params.pageSize);
    this.tai();
  }

  protected moForm(dot: DotDatnItem | null): void {
    this.dangSua.set(dot);
    this.form.reset(
      dot
        ? {
            maDot: dot.maDot,
            tenDot: dot.tenDot,
            namHoc: dot.namHoc,
            hocKy: dot.hocKy,
            ngayBatDau: dot.ngayBatDau,
            soTuan: dot.soTuan,
            ngayNopQuyenHan: dot.ngayNopQuyenHan,
          }
        : undefined,
    );
    this.formMo.set(true);
  }

  protected luu(): void {
    const coSoId = this.dangSua()?.coSoId ?? this.auth.coSoDangLamViec()?.id;
    if (this.form.invalid || !coSoId) {
      return;
    }
    const body: DotDatnRequest = { ...this.form.getRawValue(), coSoId };
    const dot = this.dangSua();
    const req$ = dot ? this.api.dotDatnSua({ id: dot.id, body }) : this.api.dotDatnTao({ body });
    this.dangLuu.set(true);
    req$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.message.success(dot ? 'Đã cập nhật đợt.' : 'Đã mở đợt mới.');
        this.formMo.set(false);
        this.dangLuu.set(false);
        this.tai();
      },
      error: () => this.dangLuu.set(false),
    });
  }

  private tai(): void {
    this.dangTai.set(true);
    this.api
      .dotDatnDanhSach({ q: this.q() || undefined, page: this.page(), pageSize: this.pageSize() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.items.set(res.items);
          this.total.set(res.total);
          this.dangTai.set(false);
        },
        error: () => this.dangTai.set(false),
      });
  }
}
