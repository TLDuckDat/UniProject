import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormControl,
  FormGroup,
  FormsModule,
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
import { NzSpinModule } from 'ng-zorro-antd/spin';

import {
  BangServerComponent,
  CotBang,
  TruyVanBang,
} from '../../shared/components/bang-server/bang-server.component';
import { CatalogItem, CatalogRequest, QuanTriApiService } from './quan-tri-api.service';

interface DanhMucLoai {
  key: string;
  label: string;
}
interface FormControls {
  ma: FormControl<string>;
  ten: FormControl<string>;
  parentId: FormControl<number | null>;
  coSoId: FormControl<number | null>;
  email: FormControl<string>;
  loai: FormControl<string>;
  soLuong: FormControl<number | null>;
  soTinChi: FormControl<number | null>;
  nganh: FormControl<string>;
  conHoatDong: FormControl<boolean>;
  tinhDiem: FormControl<boolean>;
  hocKyKeHoach: FormControl<number | null>;
  laHPThayTheDATN: FormControl<boolean>;
  he: FormControl<string>;
  chuongTrinh: FormControl<string>;
  tongTCYeuCau: FormControl<number | null>;
  tcdatn: FormControl<number | null>;
  tcttn: FormControl<number | null>;
  coHocPhanThayThe: FormControl<boolean>;
  hocPhanId: FormControl<number | null>;
}

@Component({
  selector: 'app-danh-muc-page',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    NzButtonModule,
    NzDrawerModule,
    NzFormModule,
    NzInputModule,
    NzSelectModule,
    NzSpinModule,
    BangServerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="page-head">
      <h1>Danh mục</h1>
      <nz-select [ngModel]="loai()" (ngModelChange)="doiLoai($event)" aria-label="Loại danh mục">
        @for (item of danhMuc; track item.key) {
          <nz-option [nzValue]="item.key" [nzLabel]="item.label" />
        }
      </nz-select>
      <button nz-button nzType="primary" (click)="moForm()">Thêm mới</button>
    </section>
    <app-bang-server
      [columns]="columns()"
      [rows]="rows()"
      [total]="total()"
      [page]="page()"
      [pageSize]="pageSize()"
      [loading]="loading()"
      [resetToken]="loai()"
      (truyVan)="tai($event)"
      (suaBanGhi)="moFormBanGhi($event)"
      (xoaBanGhi)="xoa($event)"
    />

    <nz-drawer
      [nzVisible]="drawer()"
      [nzTitle]="editing() ? 'Sửa danh mục' : 'Thêm danh mục'"
      [nzWidth]="540"
      (nzOnClose)="dongForm()"
    >
      <ng-container *nzDrawerContent>
        <form nz-form [formGroup]="form" (ngSubmit)="luu()">
          <nz-form-item
            ><nz-form-label nzRequired>Mã / tên phòng</nz-form-label
            ><nz-form-control><input nz-input formControlName="ma" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label nzRequired>Tên</nz-form-label
            ><nz-form-control><input nz-input formControlName="ten" /></nz-form-control
          ></nz-form-item>
          @if (hasParent()) {
            <nz-form-item
              ><nz-form-label>ID cha</nz-form-label
              ><nz-form-control
                ><input nz-input type="number" formControlName="parentId" /></nz-form-control
            ></nz-form-item>
          }
          @if (loai() === 'khoa') {
            <nz-form-item
              ><nz-form-label>Khối ngành</nz-form-label
              ><nz-form-control
                ><nz-select formControlName="loai"
                  ><nz-option nzValue="KyThuat" nzLabel="Kỹ thuật" /><nz-option
                    nzValue="KinhTe"
                    nzLabel="Kinh tế" /></nz-select></nz-form-control
            ></nz-form-item>
          }
          @if (loai() === 'giang-vien') {
            <nz-form-item
              ><nz-form-label>Cơ sở ID</nz-form-label
              ><nz-form-control
                ><input nz-input type="number" formControlName="coSoId" /></nz-form-control
            ></nz-form-item>
            <nz-form-item
              ><nz-form-label>Email</nz-form-label
              ><nz-form-control><input nz-input formControlName="email" /></nz-form-control
            ></nz-form-item>
            <nz-form-item
              ><nz-form-label>Loại giảng viên</nz-form-label
              ><nz-form-control
                ><nz-select formControlName="loai"
                  ><nz-option nzValue="CoHuu" nzLabel="Cơ hữu" /><nz-option
                    nzValue="ThinhGiang"
                    nzLabel="Thỉnh giảng" /><nz-option
                    nzValue="NgoaiTruong"
                    nzLabel="Ngoài trường" /></nz-select></nz-form-control
            ></nz-form-item>
            <nz-form-item
              ><nz-form-label>Quota tối đa</nz-form-label
              ><nz-form-control
                ><input nz-input type="number" formControlName="soLuong" /></nz-form-control
            ></nz-form-item>
          }
          @if (loai() === 'phong-bao-ve') {
            <nz-form-item
              ><nz-form-label>Sức chứa</nz-form-label
              ><nz-form-control
                ><input nz-input type="number" formControlName="soLuong" /></nz-form-control
            ></nz-form-item>
          }
          @if (loai() === 'hoc-phan') {
            <nz-form-item
              ><nz-form-label>Số tín chỉ</nz-form-label
              ><nz-form-control
                ><input nz-input type="number" formControlName="soTinChi" /></nz-form-control
            ></nz-form-item>
            <nz-form-item
              ><nz-form-label>Loại học phần</nz-form-label
              ><nz-form-control
                ><nz-select formControlName="loai"
                  ><nz-option nzValue="GDQP" nzLabel="GDQP" /><nz-option
                    nzValue="GDTC"
                    nzLabel="GDTC" /><nz-option nzValue="TTTN" nzLabel="TTTN" /><nz-option
                    nzValue="DATN"
                    nzLabel="ĐATN" /><nz-option
                    nzValue="Thuong"
                    nzLabel="Thường" /></nz-select></nz-form-control
            ></nz-form-item>
          }
          @if (loai() === 'ctdt-hoc-phan') {
            <nz-form-item
              ><nz-form-label>Học phần ID</nz-form-label
              ><nz-form-control
                ><input nz-input type="number" formControlName="hocPhanId" /></nz-form-control
            ></nz-form-item>
            <nz-form-item
              ><nz-form-label>Học kỳ kế hoạch</nz-form-label
              ><nz-form-control
                ><input nz-input type="number" formControlName="hocKyKeHoach" /></nz-form-control
            ></nz-form-item>
          }
          @if (loai() === 'chuong-trinh-dao-tao') {
            <nz-form-item
              ><nz-form-label>Ngành</nz-form-label
              ><nz-form-control><input nz-input formControlName="nganh" /></nz-form-control
            ></nz-form-item>
            <nz-form-item
              ><nz-form-label>Hệ đào tạo</nz-form-label
              ><nz-form-control
                ><nz-select formControlName="he"
                  ><nz-option nzValue="CuNhan" nzLabel="Cử nhân" /><nz-option
                    nzValue="KySuChuyenSau"
                    nzLabel="Kỹ sư chuyên sâu" /></nz-select></nz-form-control
            ></nz-form-item>
            <nz-form-item
              ><nz-form-label>Chương trình</nz-form-label
              ><nz-form-control
                ><nz-select formControlName="chuongTrinh"
                  ><nz-option nzValue="DaiTra" nzLabel="Đại trà" /><nz-option
                    nzValue="CLC"
                    nzLabel="CLC" /><nz-option
                    nzValue="TienTien"
                    nzLabel="Tiên tiến" /></nz-select></nz-form-control
            ></nz-form-item>
            <nz-form-item
              ><nz-form-label>Tổng TC yêu cầu</nz-form-label
              ><nz-form-control
                ><input nz-input type="number" formControlName="tongTCYeuCau" /></nz-form-control
            ></nz-form-item>
            <nz-form-item
              ><nz-form-label>TC ĐATN</nz-form-label
              ><nz-form-control
                ><input nz-input type="number" formControlName="tcdatn" /></nz-form-control
            ></nz-form-item>
            <nz-form-item
              ><nz-form-label>TC tốt nghiệp</nz-form-label
              ><nz-form-control
                ><input nz-input type="number" formControlName="tcttn" /></nz-form-control
            ></nz-form-item>
          }
          <button nz-button nzType="primary" [disabled]="form.invalid || loading()">Lưu</button>
          <button nz-button type="button" (click)="dongForm()">Hủy</button>
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
      gap: 12px;
      margin-bottom: 16px;
    }
    .page-head h1 {
      margin: 0 auto 0 0;
    }
  `,
})
export class DanhMucPage {
  private readonly api = inject(QuanTriApiService);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly message = inject(NzMessageService);
  readonly danhMuc: DanhMucLoai[] = [
    { key: 'coso', label: 'Cơ sở' },
    { key: 'khoa', label: 'Khoa' },
    { key: 'bo-mon', label: 'Bộ môn' },
    { key: 'giang-vien', label: 'Giảng viên' },
    { key: 'phong-bao-ve', label: 'Phòng bảo vệ' },
    { key: 'chuong-trinh-dao-tao', label: 'Chương trình đào tạo' },
    { key: 'hoc-phan', label: 'Học phần' },
    { key: 'ctdt-hoc-phan', label: 'Học phần trong CTĐT' },
  ];
  readonly loai = signal('coso');
  readonly rows = signal<Record<string, unknown>[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly loading = signal(false);
  readonly drawer = signal(false);
  readonly editing = signal<number | null>(null);
  private currentQuery: TruyVanBang = { page: 1, pageSize: 20, desc: false, q: '' };
  readonly columns = signal<CotBang[]>([
    { key: 'ma', label: 'Mã', sortable: true },
    { key: 'ten', label: 'Tên', sortable: true },
    { key: 'parentTen', label: 'Thuộc' },
    { key: 'loai', label: 'Loại' },
    { key: 'soLuong', label: 'Quota / Sức chứa' },
    { key: 'email', label: 'Email' },
  ]);
  readonly form: FormGroup<FormControls> = this.fb.group({
    ma: this.fb.control('', Validators.required),
    ten: this.fb.control('', Validators.required),
    parentId: new FormControl<number | null>(null),
    coSoId: new FormControl<number | null>(null),
    email: this.fb.control(''),
    loai: this.fb.control(''),
    soLuong: new FormControl<number | null>(null),
    soTinChi: new FormControl<number | null>(null),
    nganh: this.fb.control(''),
    conHoatDong: this.fb.control(true),
    tinhDiem: this.fb.control(true),
    hocKyKeHoach: new FormControl<number | null>(null),
    laHPThayTheDATN: this.fb.control(false),
    he: this.fb.control('CuNhan'),
    chuongTrinh: this.fb.control('DaiTra'),
    tongTCYeuCau: new FormControl<number | null>(null),
    tcdatn: new FormControl<number | null>(null),
    tcttn: new FormControl<number | null>(null),
    coHocPhanThayThe: this.fb.control(false),
    hocPhanId: new FormControl<number | null>(null),
  });

  constructor() {
    this.tai(this.currentQuery);
  }
  protected hasParent(): boolean {
    return [
      'khoa',
      'bo-mon',
      'giang-vien',
      'phong-bao-ve',
      'chuong-trinh-dao-tao',
      'ctdt-hoc-phan',
    ].includes(this.loai());
  }
  protected doiLoai(value: string): void {
    this.loai.set(value);
    this.currentQuery = { page: 1, pageSize: 20, desc: false, q: '' };
    this.tai(this.currentQuery);
  }
  protected tai(query: TruyVanBang): void {
    this.currentQuery = query;
    this.loading.set(true);
    this.api
      .danhMuc(this.loai(), query.q, query.page, query.pageSize, query.sort, query.desc)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.rows.set(result.items as unknown as Record<string, unknown>[]);
          this.total.set(result.total);
          this.page.set(result.page);
          this.pageSize.set(result.pageSize);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }
  protected moForm(item?: CatalogItem): void {
    this.editing.set(item?.id ?? null);
    this.form.reset({
      ma: item?.ma ?? '',
      ten: item?.ten ?? '',
      parentId: item?.parentId ?? null,
      coSoId: item?.coSoId ?? null,
      email: item?.email ?? '',
      loai: item?.loai ?? '',
      soLuong: item?.soLuong ?? null,
      soTinChi: item?.soTinChi ?? null,
      nganh: item?.nganh ?? '',
      conHoatDong: item?.conHoatDong ?? true,
      tinhDiem: item?.tinhDiem ?? true,
      hocKyKeHoach: item?.hocKyKeHoach ?? null,
      laHPThayTheDATN: item?.laHPThayTheDATN ?? false,
      he: item?.he ?? 'CuNhan',
      chuongTrinh: item?.chuongTrinh ?? 'DaiTra',
      tongTCYeuCau: item?.tongTCYeuCau ?? null,
      tcdatn: item?.tcdatn ?? null,
      tcttn: item?.tcttn ?? null,
      coHocPhanThayThe: item?.coHocPhanThayThe ?? false,
      hocPhanId: item?.hocPhanId ?? null,
    });
    this.drawer.set(true);
  }
  protected moFormBanGhi(row: Record<string, unknown>): void {
    this.moForm(row as unknown as CatalogItem);
  }
  protected xoa(row: Record<string, unknown>): void {
    const item = row as unknown as CatalogItem;
    this.api
      .xoaDanhMuc(this.loai(), item.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.message.success('Đã ngừng/xóa bản ghi.');
          this.tai(this.currentQuery);
        },
      });
  }
  protected dongForm(): void {
    this.drawer.set(false);
  }
  protected luu(): void {
    if (this.form.invalid) return;
    const request = this.form.getRawValue() as CatalogRequest;
    const id = this.editing();
    const call =
      id === null
        ? this.api.taoDanhMuc(this.loai(), request)
        : this.api.suaDanhMuc(this.loai(), id, request);
    call.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.message.success('Đã lưu danh mục.');
        this.dongForm();
        this.tai(this.currentQuery);
      },
    });
  }
}
