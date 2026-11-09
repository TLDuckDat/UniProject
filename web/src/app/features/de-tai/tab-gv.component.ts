import { DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  input,
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
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzEmptyModule } from 'ng-zorro-antd/empty';

import type { BoMonTomTat } from '../../api/models/bo-mon-tom-tat';
import type { DangKyNguyenVongItem } from '../../api/models/dang-ky-nguyen-vong-item';
import type { DeTaiItem } from '../../api/models/de-tai-item';
import { DeTaiService } from '../../api/services/de-tai.service';
import {
  LOAI_DE_TAI_LUA_CHON,
  nhanLoaiDeTai,
  nhanNguyenVong,
  nhanTrangThaiDeTai,
} from './de-tai-nhan';

interface DeTaiControls {
  tenDeTai: FormControl<string>;
  moTa: FormControl<string>;
  boMonId: FormControl<number | null>;
  loaiDeTai: FormControl<string>;
  maxSV: FormControl<number>;
}

/** Tab GV: đề tài của tôi, xác nhận đề xuất của SV (B04), đề xuất đề tài mới (B01, B02, B10), duyệt SV đăng ký. */
@Component({
  selector: 'app-tab-gv',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DecimalPipe,
    ReactiveFormsModule,
    NzAlertModule,
    NzButtonModule,
    NzDrawerModule,
    NzEmptyModule,
    NzFormModule,
    NzInputModule,
    NzInputNumberModule,
    NzSelectModule,
    NzTableModule,
    NzTagModule,
  ],
  template: `
    <div class="thanh">
      <button nz-button nzType="primary" (click)="moForm()">Đề xuất đề tài mới</button>
    </div>

    <nz-table
      #bang
      nzSize="middle"
      [nzData]="items()"
      [nzLoading]="dangTai()"
      [nzShowPagination]="false"
    >
      <thead>
        <tr>
          <th>Đề tài</th>
          <th>Bộ môn</th>
          <th>Loại</th>
          <th nzAlign="right">SV</th>
          <th>Trạng thái</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        @for (d of bang.data; track d.id) {
          <tr>
            <td>
              {{ d.tenDeTai }}
              @if (d.canhBaoTrung) {
                <nz-tag nzColor="gold">Trùng đề tài năm trước</nz-tag>
              }
              @if (d.nguonDeXuat === 'SV') {
                <nz-tag nzColor="purple">SV đề xuất: {{ d.maSVDeXuat }}</nz-tag>
              }
            </td>
            <td>{{ d.maBoMon }}</td>
            <td>{{ loai(d.loaiDeTai) }}</td>
            <td class="so">{{ d.soSVDaNhan }}/{{ d.maxSV }}</td>
            <td>
              <nz-tag [nzColor]="trangThai(d.trangThaiDuyet).color">{{
                trangThai(d.trangThaiDuyet).label
              }}</nz-tag>
            </td>
            <td class="hanh-dong">
              @if (d.trangThaiDuyet === 'Nhap') {
                <button nz-button nzType="link" (click)="xacNhan(d, true)">Nhận hướng dẫn</button>
                <button nz-button nzType="link" nzDanger (click)="xacNhan(d, false)">
                  Từ chối
                </button>
              }
              @if (d.trangThaiDuyet === 'DaDuyet') {
                <button nz-button nzType="link" (click)="moDangKy(d)">Duyệt SV đăng ký</button>
              }
            </td>
          </tr>
        } @empty {
          <tr>
            <td colspan="6"><nz-empty nzNotFoundContent="Chưa có đề tài nào trong đợt này." /></td>
          </tr>
        }
      </tbody>
    </nz-table>

    <nz-drawer
      [nzVisible]="formMo()"
      [nzWidth]="480"
      nzTitle="Đề xuất đề tài mới"
      (nzOnClose)="formMo.set(false)"
    >
      <ng-container *nzDrawerContent>
        <form nz-form nzLayout="vertical" [formGroup]="form" (ngSubmit)="luu()">
          <nz-form-item>
            <nz-form-label nzRequired>Tên đề tài</nz-form-label>
            <nz-form-control nzErrorTip="Từ 1 đến 500 ký tự">
              <input nz-input formControlName="tenDeTai" maxlength="500" />
            </nz-form-control>
          </nz-form-item>
          <nz-form-item>
            <nz-form-label>Mô tả</nz-form-label>
            <nz-form-control>
              <textarea nz-input formControlName="moTa" rows="3" maxlength="4000"></textarea>
            </nz-form-control>
          </nz-form-item>
          <nz-form-item>
            <nz-form-label nzRequired>Bộ môn</nz-form-label>
            <nz-form-control nzErrorTip="Chọn bộ môn">
              <nz-select formControlName="boMonId" nzPlaceHolder="Chọn bộ môn">
                @for (b of boMons(); track b.id) {
                  <nz-option [nzValue]="b.id" [nzLabel]="b.tenBM + ' (' + b.tenKhoa + ')'" />
                }
              </nz-select>
            </nz-form-control>
          </nz-form-item>
          <nz-form-item>
            <nz-form-label nzRequired>Loại đề tài</nz-form-label>
            <nz-form-control>
              <nz-select formControlName="loaiDeTai">
                @for (l of loaiLuaChon; track l.value) {
                  <nz-option [nzValue]="l.value" [nzLabel]="l.label" />
                }
              </nz-select>
            </nz-form-control>
          </nz-form-item>
          <nz-form-item>
            <nz-form-label nzRequired>Số SV tối đa</nz-form-label>
            <nz-form-control
              nzExtra="Mặc định 1. Nhóm 2–3 SV chỉ với đề tài chế tạo / hệ thống và Bộ môn duyệt."
            >
              <nz-input-number formControlName="maxSV" [nzMin]="1" [nzMax]="3" [nzStep]="1" />
            </nz-form-control>
          </nz-form-item>
          <button
            nz-button
            nzType="primary"
            type="submit"
            [disabled]="form.invalid"
            [nzLoading]="dangLuu()"
          >
            Gửi Bộ môn duyệt
          </button>
        </form>
        @if (canhBao(); as trung) {
          @if (trung.length > 0) {
            <nz-alert
              class="mt"
              nzType="warning"
              nzShowIcon
              nzMessage="Đề tài này giống đề tài các đợt trước"
              [nzDescription]="moTaTrung"
            />
            <ng-template #moTaTrung>
              <ul>
                @for (t of trung; track t.id) {
                  <li>
                    {{ t.tenDeTai }} – {{ t.maDot }} ({{ t.namHoc }}), độ giống
                    {{ t.doTuongDong * 100 | number: '1.0-0' }}%
                  </li>
                }
              </ul>
            </ng-template>
          }
        }
      </ng-container>
    </nz-drawer>

    <nz-drawer
      [nzVisible]="dangKyCua() !== null"
      [nzWidth]="640"
      [nzTitle]="'Sinh viên đăng ký – ' + (dangKyCua()?.tenDeTai ?? '')"
      (nzOnClose)="dangKyCua.set(null)"
    >
      <ng-container *nzDrawerContent>
        <p>Đề tài đủ {{ dangKyCua()?.maxSV }} SV sẽ bị khóa khi duyệt đủ số lượng.</p>
        <nz-table
          nzSize="small"
          [nzData]="dangKy()"
          [nzShowPagination]="false"
          [nzLoading]="dangTaiDangKy()"
        >
          <thead>
            <tr>
              <th>Mã SV</th>
              <th>Họ tên</th>
              <th nzAlign="right">Thứ tự</th>
              <th>Trạng thái</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (k of dangKy(); track k.id) {
              <tr>
                <td>{{ k.maSV }}</td>
                <td>{{ k.hoTen }}</td>
                <td class="so">{{ k.thuTuNguyenVong }}</td>
                <td>
                  <nz-tag [nzColor]="nguyenVong(k.trangThai).color">{{
                    nguyenVong(k.trangThai).label
                  }}</nz-tag>
                </td>
                <td class="hanh-dong">
                  @if (k.trangThai === 'ChoDuyet') {
                    <button nz-button nzType="link" (click)="duyetSv(k, true)">Trúng</button>
                    <button nz-button nzType="link" nzDanger (click)="duyetSv(k, false)">
                      Trượt
                    </button>
                  }
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="5"><nz-empty nzNotFoundContent="Chưa có sinh viên đăng ký." /></td>
              </tr>
            }
          </tbody>
        </nz-table>
      </ng-container>
    </nz-drawer>
  `,
  styles: `
    .thanh {
      margin-bottom: 12px;
    }
    .so {
      text-align: right;
    }
    .mt {
      margin-top: 12px;
    }
    .hanh-dong button {
      padding: 0 6px;
    }
  `,
})
export class TabGvComponent implements OnInit {
  readonly dotId = input.required<number>();

  private readonly api = inject(DeTaiService);
  private readonly message = inject(NzMessageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loai = nhanLoaiDeTai;
  protected readonly trangThai = nhanTrangThaiDeTai;
  protected readonly nguyenVong = nhanNguyenVong;
  protected readonly loaiLuaChon = LOAI_DE_TAI_LUA_CHON;

  protected readonly items = signal<DeTaiItem[]>([]);
  protected readonly dangTai = signal(false);
  protected readonly boMons = signal<BoMonTomTat[]>([]);
  protected readonly formMo = signal(false);
  protected readonly dangLuu = signal(false);
  protected readonly canhBao = signal<DeTaiItem['trungVoi'] | null>(null);
  protected readonly dangKyCua = signal<DeTaiItem | null>(null);
  protected readonly dangKy = signal<DangKyNguyenVongItem[]>([]);
  protected readonly dangTaiDangKy = signal(false);

  protected readonly form: FormGroup<DeTaiControls> = inject(NonNullableFormBuilder).group({
    tenDeTai: ['', [Validators.required, Validators.maxLength(500)]],
    moTa: [''],
    boMonId: new FormControl<number | null>(null, Validators.required),
    loaiDeTai: ['LyThuyet', Validators.required],
    maxSV: [1, [Validators.required, Validators.min(1), Validators.max(3)]],
  });

  ngOnInit(): void {
    this.tai();
    this.api
      .deTaiBoMon({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((b) => this.boMons.set(b));
  }

  protected moForm(): void {
    this.form.reset({ tenDeTai: '', moTa: '', boMonId: null, loaiDeTai: 'LyThuyet', maxSV: 1 });
    this.canhBao.set(null);
    this.formMo.set(true);
  }

  protected luu(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.dangLuu.set(true);
    this.api
      .deTaiGvDeXuat({
        body: {
          dotId: this.dotId(),
          boMonId: v.boMonId!,
          tenDeTai: v.tenDeTai.trim(),
          moTa: v.moTa.trim() || null,
          loaiDeTai: v.loaiDeTai as never,
          maxSV: v.maxSV,
        },
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (d) => {
          this.dangLuu.set(false);
          this.message.success('Đã gửi đề tài cho Bộ môn duyệt.');
          this.canhBao.set(d.trungVoi);
          this.tai();
          if (d.trungVoi.length === 0) this.formMo.set(false);
        },
        error: () => this.dangLuu.set(false),
      });
  }

  protected xacNhan(d: DeTaiItem, dongY: boolean): void {
    this.api
      .deTaiGvXacNhan({ id: d.id, body: { dongY } })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.message.success(
          dongY ? 'Đã nhận hướng dẫn; chờ Bộ môn duyệt.' : 'Đã từ chối đề xuất của sinh viên.',
        );
        this.tai();
      });
  }

  protected moDangKy(d: DeTaiItem): void {
    this.dangKyCua.set(d);
    this.taiDangKy(d.id);
  }

  protected duyetSv(k: DangKyNguyenVongItem, dongY: boolean): void {
    const deTai = this.dangKyCua();
    if (!deTai) return;
    this.api
      .deTaiGvDuyetDangKy({ id: deTai.id, body: { svTrongDotId: k.svTrongDotId, dongY } })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.message.success(dongY ? 'Đã duyệt trúng tuyển.' : 'Đã đánh dấu trượt.');
        this.taiDangKy(deTai.id);
        this.tai();
      });
  }

  private taiDangKy(deTaiId: number): void {
    this.dangTaiDangKy.set(true);
    this.api
      .deTaiDanhSachDangKy({ id: deTaiId })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (ds) => {
          this.dangKy.set(ds);
          this.dangTaiDangKy.set(false);
        },
        error: () => this.dangTaiDangKy.set(false),
      });
  }

  private tai(): void {
    this.dangTai.set(true);
    this.api
      .deTaiCuaToi({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (ds) => {
          this.items.set(ds);
          this.dangTai.set(false);
        },
        error: () => this.dangTai.set(false),
      });
  }
}
