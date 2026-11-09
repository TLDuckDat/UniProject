import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormControl,
  FormGroup,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSelectModule } from 'ng-zorro-antd/select';

import {
  BangServerComponent,
  CotBang,
  TruyVanBang,
} from '../../shared/components/bang-server/bang-server.component';
import { AccountItem, AccountRequest, QuanTriApiService } from './quan-tri-api.service';

interface AccountControls {
  tenDangNhap: FormControl<string>;
  email: FormControl<string>;
  matKhau: FormControl<string>;
  loaiTK: FormControl<string>;
  conHoatDong: FormControl<boolean>;
  giangVienId: FormControl<number | null>;
  sinhVienId: FormControl<number | null>;
  vaiTroText: FormControl<string>;
}

@Component({
  selector: 'app-tai-khoan-quan-ly-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    NzButtonModule,
    NzDrawerModule,
    NzFormModule,
    NzInputModule,
    NzSelectModule,
    BangServerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="page-head">
      <h1>Tài khoản & phân quyền</h1>
      <button nz-button nzType="primary" (click)="moForm()">Tạo tài khoản</button>
    </section>
    <app-bang-server
      [columns]="columns"
      [rows]="rows()"
      [total]="total()"
      [page]="page()"
      [pageSize]="pageSize()"
      [loading]="loading()"
      (truyVan)="tai($event)"
      (suaBanGhi)="sua($event)"
      (xoaBanGhi)="xoa($event)"
    />
    <nz-drawer
      [nzVisible]="drawer()"
      [nzTitle]="editing() ? 'Sửa tài khoản' : 'Tạo tài khoản'"
      [nzWidth]="560"
      (nzOnClose)="drawer.set(false)"
    >
      <ng-container *nzDrawerContent>
        <form nz-form [formGroup]="form" (ngSubmit)="luu()">
          <nz-form-item
            ><nz-form-label nzRequired>Tên đăng nhập</nz-form-label
            ><nz-form-control><input nz-input formControlName="tenDangNhap" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Email</nz-form-label
            ><nz-form-control
              ><input nz-input type="email" formControlName="email" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>{{
              editing() ? 'Mật khẩu mới (để trống nếu giữ nguyên)' : 'Mật khẩu'
            }}</nz-form-label
            ><nz-form-control
              ><input nz-input type="password" formControlName="matKhau" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Loại tài khoản</nz-form-label
            ><nz-form-control
              ><nz-select formControlName="loaiTK"
                ><nz-option nzValue="NoiBo" nzLabel="Nội bộ" /><nz-option
                  nzValue="SV"
                  nzLabel="Sinh viên" /><nz-option nzValue="GV" nzLabel="Giảng viên" /><nz-option
                  nzValue="HeThongNgoai"
                  nzLabel="Hệ thống ngoài" /></nz-select></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Giảng viên ID</nz-form-label
            ><nz-form-control
              ><input nz-input type="number" formControlName="giangVienId" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Sinh viên ID</nz-form-label
            ><nz-form-control
              ><input nz-input type="number" formControlName="sinhVienId" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label nzRequired
              >Vai trò và phạm vi (mỗi dòng: VaiTro hoặc VaiTro|CoSo/Khoa/BoMon|ID)</nz-form-label
            ><nz-form-control>
              <textarea
                nz-input
                rows="5"
                formControlName="vaiTroText"
                placeholder="QuanTri&#10;PhongDaoTao|CoSo|1&#10;TruongBoMon|BoMon|12"
              ></textarea></nz-form-control
          ></nz-form-item>
          <label><input type="checkbox" formControlName="conHoatDong" /> Đang hoạt động</label>
          <div class="actions">
            <button nz-button nzType="primary" [disabled]="form.invalid">Lưu</button
            ><button nz-button type="button" (click)="drawer.set(false)">Hủy</button>
          </div>
        </form>
      </ng-container>
    </nz-drawer>
  `,
  styles: `
    :host {
      display: block;
      padding: 20px;
    }
    .page-head {
      display: flex;
      align-items: center;
      margin-bottom: 16px;
    }
    .page-head h1 {
      margin-right: auto;
    }
    .actions {
      display: flex;
      gap: 8px;
      margin-top: 16px;
    }
  `,
})
export class TaiKhoanQuanLyPage {
  private readonly api = inject(QuanTriApiService);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly message = inject(NzMessageService);
  readonly columns: CotBang[] = [
    { key: 'tenDangNhap', label: 'Tài khoản', sortable: true },
    { key: 'email', label: 'Email' },
    { key: 'loaiTK', label: 'Loại' },
    { key: 'vaiTroText', label: 'Vai trò / phạm vi' },
    { key: 'conHoatDong', label: 'Hoạt động' },
  ];
  readonly rows = signal<Record<string, unknown>[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly loading = signal(false);
  readonly drawer = signal(false);
  readonly editing = signal<number | null>(null);
  private query: TruyVanBang = { page: 1, pageSize: 20, desc: false, q: '' };
  readonly form: FormGroup<AccountControls> = this.fb.group({
    tenDangNhap: this.fb.control('', Validators.required),
    email: this.fb.control(''),
    matKhau: this.fb.control(''),
    loaiTK: this.fb.control('NoiBo'),
    conHoatDong: this.fb.control(true),
    giangVienId: new FormControl<number | null>(null),
    sinhVienId: new FormControl<number | null>(null),
    vaiTroText: this.fb.control(''),
  });
  constructor() {
    this.tai(this.query);
  }
  protected tai(query: TruyVanBang): void {
    this.query = query;
    this.loading.set(true);
    this.api
      .taiKhoans(query.q, query.page, query.pageSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.rows.set(
            result.items.map((x) => ({
              ...x,
              vaiTroText: x.vaiTro
                .map(
                  (r) =>
                    `${r.maVaiTro}${r.boMonId ? ` · BM ${r.boMonId}` : r.khoaId ? ` · Khoa ${r.khoaId}` : r.coSoId ? ` · CS ${r.coSoId}` : ' · Toàn hệ thống'}`,
                )
                .join(', '),
            })) as unknown as Record<string, unknown>[],
          );
          this.total.set(result.total);
          this.page.set(result.page);
          this.pageSize.set(result.pageSize);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }
  protected sua(row: Record<string, unknown>): void {
    const item = row as unknown as AccountItem;
    this.editing.set(item.id);
    this.form.reset({
      tenDangNhap: item.tenDangNhap,
      email: item.email ?? '',
      matKhau: '',
      loaiTK: item.loaiTK,
      conHoatDong: item.conHoatDong,
      giangVienId: item.giangVienId ?? null,
      sinhVienId: item.sinhVienId ?? null,
      vaiTroText: item.vaiTro
        .map(
          (r) =>
            `${r.maVaiTro}${r.boMonId ? `|BoMon|${r.boMonId}` : r.khoaId ? `|Khoa|${r.khoaId}` : r.coSoId ? `|CoSo|${r.coSoId}` : ''}`,
        )
        .join('\n'),
    });
    this.drawer.set(true);
  }
  protected moForm(): void {
    this.editing.set(null);
    this.form.reset({
      tenDangNhap: '',
      email: '',
      matKhau: '',
      loaiTK: 'NoiBo',
      conHoatDong: true,
      giangVienId: null,
      sinhVienId: null,
      vaiTroText: '',
    });
    this.drawer.set(true);
  }
  protected xoa(row: Record<string, unknown>): void {
    this.api
      .xoaTaiKhoan(Number(row['id']))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.message.success('Đã ngừng tài khoản.');
          this.tai(this.query);
        },
      });
  }
  protected luu(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    let roles: AccountRequest['vaiTro'];
    try {
      roles = v.vaiTroText
        .split('\n')
        .map((line) => line.trim())
        .filter(Boolean)
        .map((line) => {
          const parts = line.split('|');
          const [maVaiTro, scope, rawId] = parts;
          if (
            !maVaiTro ||
            parts.length > 3 ||
            (parts.length > 1 && !['CoSo', 'Khoa', 'BoMon'].includes(scope ?? ''))
          ) {
            throw new Error(`Dòng vai trò không hợp lệ: ${line}`);
          }
          if (
            parts.length > 1 &&
            (!rawId || !Number.isSafeInteger(Number(rawId)) || Number(rawId) <= 0)
          ) {
            throw new Error(`Thiếu ID phạm vi ở dòng: ${line}`);
          }
          const id = rawId ? Number(rawId) : null;
          return {
            maVaiTro,
            coSoId: scope === 'CoSo' ? id : null,
            khoaId: scope === 'Khoa' ? id : null,
            boMonId: scope === 'BoMon' ? id : null,
          };
        });
    } catch (error) {
      this.message.error(error instanceof Error ? error.message : 'Phạm vi vai trò không hợp lệ.');
      return;
    }
    const request: AccountRequest = {
      tenDangNhap: v.tenDangNhap,
      email: v.email || null,
      matKhau: v.matKhau || null,
      loaiTK: v.loaiTK,
      conHoatDong: v.conHoatDong,
      giangVienId: v.giangVienId,
      sinhVienId: v.sinhVienId,
      vaiTro: roles,
    };
    const id = this.editing();
    const save = id === null ? this.api.taoTaiKhoan(request) : this.api.suaTaiKhoan(id, request);
    save.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.message.success('Đã lưu tài khoản.');
        this.drawer.set(false);
        this.tai(this.query);
      },
    });
  }
}
