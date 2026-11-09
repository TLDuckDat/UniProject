import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormsModule,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzEmptyModule } from 'ng-zorro-antd/empty';

import type { DeTaiItem } from '../../api/models/de-tai-item';
import type { GiangVienTomTat } from '../../api/models/giang-vien-tom-tat';
import type { NguyenVongItem } from '../../api/models/nguyen-vong-item';
import { DeTaiService } from '../../api/services/de-tai.service';
import { HoSoCuaToiService } from '../../api/services/ho-so-cua-toi.service';
import {
  LOAI_DE_TAI_LUA_CHON,
  nhanLoaiDeTai,
  nhanLoaiDon,
  nhanNguyenVong,
  nhanTrangThaiDeTai,
} from './de-tai-nhan';

const SO_NGUYEN_VONG_TOI_DA = 5;

/**
 * Tab SV: danh mục đề tài đã duyệt của đợt, đăng ký nguyện vọng theo thứ tự (tối đa 5),
 * tự đề xuất đề tài (B04), và xin đổi GVHD / đổi đề tài sau quyết định giao (B08, B09).
 */
@Component({
  selector: 'app-tab-sv',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    NzAlertModule,
    NzButtonModule,
    NzDrawerModule,
    NzEmptyModule,
    NzFormModule,
    NzInputModule,
    NzModalModule,
    NzSelectModule,
    NzTableModule,
    NzTagModule,
  ],
  template: `
    @if (!hoSoId()) {
      <nz-alert nzType="info" nzShowIcon nzMessage="Bạn chưa có hồ sơ ĐATN trong đợt này." />
    } @else {
      <div class="thanh">
        <button nz-button (click)="moTuDeXuat()">Tự đề xuất đề tài</button>
        <button nz-button (click)="moDon()">Xin đổi GVHD / đề tài</button>
      </div>

      <h4>Nguyện vọng của tôi</h4>
      @if (nguyenVong().length === 0) {
        <p class="goi-y">
          Chưa đăng ký nguyện vọng. Chọn đề tài bên dưới theo thứ tự ưu tiên (tối đa {{ toiDa }}).
        </p>
      } @else {
        <ol>
          @for (n of nguyenVong(); track n.id) {
            <li>
              {{ n.tenDeTai }}
              <nz-tag [nzColor]="nhanNV(n.trangThai).color">{{ nhanNV(n.trangThai).label }}</nz-tag>
            </li>
          }
        </ol>
      }

      <h4>Danh mục đề tài mở</h4>
      <div class="loc">
        <nz-select
          class="bo-mon"
          nzAllowClear
          nzPlaceHolder="Lọc theo bộ môn"
          [ngModel]="boMonLoc()"
          (ngModelChange)="boMonLoc.set($event); tai()"
        >
          @for (b of boMons(); track b.id) {
            <nz-option [nzValue]="b.id" [nzLabel]="b.tenBM" />
          }
        </nz-select>
        <nz-select
          class="chon-nv"
          nzMode="multiple"
          nzPlaceHolder="Chọn nguyện vọng theo thứ tự ưu tiên"
          [nzMaxMultipleCount]="toiDa"
          [ngModel]="chon()"
          (ngModelChange)="chon.set($event)"
          aria-label="Chọn nguyện vọng"
        >
          @for (d of cocChon(); track d.id) {
            <nz-option [nzValue]="d.id" [nzLabel]="d.tenDeTai + ' – ' + d.tenBoMon" />
          }
        </nz-select>
        <button
          nz-button
          nzType="primary"
          [disabled]="chon().length === 0"
          [nzLoading]="dangGui()"
          (click)="guiNguyenVong()"
        >
          Gửi nguyện vọng
        </button>
      </div>

      <nz-table
        #bang
        nzSize="middle"
        [nzData]="deTais()"
        [nzLoading]="dangTai()"
        [nzShowPagination]="false"
      >
        <thead>
          <tr>
            <th>Đề tài</th>
            <th>Bộ môn</th>
            <th>GV đề xuất</th>
            <th>Loại</th>
            <th nzAlign="right">Còn chỗ</th>
            <th>Trạng thái</th>
          </tr>
        </thead>
        <tbody>
          @for (d of bang.data; track d.id) {
            <tr>
              <td>{{ d.tenDeTai }}</td>
              <td>{{ d.tenBoMon }}</td>
              <td>{{ d.tenGVDeXuat ?? '—' }}</td>
              <td>{{ loai(d.loaiDeTai) }}</td>
              <td class="so">{{ d.maxSV - d.soSVDaNhan }}/{{ d.maxSV }}</td>
              <td>
                @if (d.daDu) {
                  <nz-tag nzColor="default">Đã đủ SV</nz-tag>
                } @else {
                  <nz-tag [nzColor]="trangThai(d.trangThaiDuyet).color">{{
                    trangThai(d.trangThaiDuyet).label
                  }}</nz-tag>
                }
              </td>
            </tr>
          } @empty {
            <tr>
              <td colspan="6">
                <nz-empty nzNotFoundContent="Chưa có đề tài đã duyệt trong đợt này." />
              </td>
            </tr>
          }
        </tbody>
      </nz-table>
    }

    <nz-drawer
      [nzVisible]="formDeXuat()"
      [nzWidth]="480"
      nzTitle="Tự đề xuất đề tài"
      (nzOnClose)="formDeXuat.set(false)"
    >
      <ng-container *nzDrawerContent>
        <p class="goi-y">Đề xuất sẽ đi qua giảng viên được chọn xác nhận, rồi Bộ môn duyệt.</p>
        <form nz-form nzLayout="vertical" [formGroup]="formDX" (ngSubmit)="guiDeXuat()">
          <nz-form-item>
            <nz-form-label nzRequired>Giảng viên đề nghị</nz-form-label>
            <nz-form-control nzErrorTip="Chọn giảng viên">
              <nz-select
                formControlName="giangVienDeNghiId"
                nzShowSearch
                nzPlaceHolder="Chọn giảng viên"
              >
                @for (g of gvs(); track g.id) {
                  <nz-option [nzValue]="g.id" [nzLabel]="g.maGV + ' – ' + g.hoTen" />
                }
              </nz-select>
            </nz-form-control>
          </nz-form-item>
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
            <nz-form-label nzRequired>Loại đề tài</nz-form-label>
            <nz-form-control>
              <nz-select formControlName="loaiDeTai">
                @for (l of loaiLuaChon; track l.value) {
                  <nz-option [nzValue]="l.value" [nzLabel]="l.label" />
                }
              </nz-select>
            </nz-form-control>
          </nz-form-item>
          <button
            nz-button
            nzType="primary"
            type="submit"
            [disabled]="formDX.invalid"
            [nzLoading]="dangGui()"
          >
            Gửi đề xuất
          </button>
        </form>
      </ng-container>
    </nz-drawer>

    <nz-modal
      [nzVisible]="formDon()"
      nzTitle="Xin đổi GVHD hoặc đổi đề tài"
      nzOkText="Gửi đơn"
      [nzOkDisabled]="donForm.invalid"
      [nzOkLoading]="dangGui()"
      (nzOnCancel)="formDon.set(false)"
      (nzOnOk)="guiDon()"
    >
      <ng-container *nzModalContent>
        <p class="goi-y">
          Chỉ gửi được sau khi hồ sơ đã có quyết định giao. Trưởng Bộ môn sẽ xem xét và ghi quyết
          định điều chỉnh.
        </p>
        <form nz-form nzLayout="vertical" [formGroup]="donForm">
          <nz-form-item>
            <nz-form-label nzRequired>Loại đơn</nz-form-label>
            <nz-form-control>
              <nz-select formControlName="loaiDon">
                <nz-option nzValue="DoiGVHD" [nzLabel]="nhanDon('DoiGVHD')" />
                <nz-option nzValue="DoiDeTai" [nzLabel]="nhanDon('DoiDeTai')" />
              </nz-select>
            </nz-form-control>
          </nz-form-item>
          <nz-form-item>
            <nz-form-label nzRequired>Lý do / nội dung</nz-form-label>
            <nz-form-control nzErrorTip="Từ 1 đến 4000 ký tự">
              <textarea nz-input formControlName="noiDung" rows="4" maxlength="4000"></textarea>
            </nz-form-control>
          </nz-form-item>
        </form>
      </ng-container>
    </nz-modal>
  `,
  styles: `
    .thanh {
      display: flex;
      gap: 8px;
      margin-bottom: 12px;
    }
    .loc {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin-bottom: 12px;
      align-items: center;
    }
    .bo-mon {
      width: 240px;
    }
    .chon-nv {
      min-width: 360px;
      flex: 1;
    }
    .so {
      text-align: right;
    }
    .goi-y {
      color: rgba(0, 0, 0, 0.55);
      font-size: 13px;
    }
    h4 {
      margin: 16px 0 8px;
    }
  `,
})
export class TabSvComponent implements OnInit {
  readonly dotId = input.required<number>();

  private readonly api = inject(DeTaiService);
  private readonly hoSoApi = inject(HoSoCuaToiService);
  private readonly message = inject(NzMessageService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly toiDa = SO_NGUYEN_VONG_TOI_DA;
  protected readonly loai = nhanLoaiDeTai;
  protected readonly trangThai = nhanTrangThaiDeTai;
  protected readonly nhanNV = nhanNguyenVong;
  protected readonly nhanDon = nhanLoaiDon;
  protected readonly loaiLuaChon = LOAI_DE_TAI_LUA_CHON;

  /** SVTrongDot của SV trong đợt này (null nếu không có hồ sơ). */
  protected readonly hoSoId = signal<number | null>(null);
  protected readonly deTais = signal<DeTaiItem[]>([]);
  protected readonly boMons = signal<{ id: number; tenBM: string }[]>([]);
  protected readonly boMonLoc = signal<number | null>(null);
  protected readonly dangTai = signal(false);
  protected readonly nguyenVong = signal<NguyenVongItem[]>([]);
  protected readonly chon = signal<number[]>([]);
  protected readonly gvs = signal<GiangVienTomTat[]>([]);
  protected readonly dangGui = signal(false);
  protected readonly formDeXuat = signal(false);
  protected readonly formDon = signal(false);

  /** Chỉ đề tài còn chỗ mới chọn làm nguyện vọng. */
  protected readonly cocChon = computed(() => this.deTais().filter((d) => !d.daDu));

  protected readonly formDX = this.fb.group({
    giangVienDeNghiId: [0, [Validators.required, Validators.min(1)]],
    tenDeTai: ['', [Validators.required, Validators.maxLength(500)]],
    moTa: [''],
    loaiDeTai: ['LyThuyet', Validators.required],
  });

  protected readonly donForm = this.fb.group({
    loaiDon: ['DoiGVHD', Validators.required],
    noiDung: ['', [Validators.required, Validators.maxLength(4000)]],
  });

  ngOnInit(): void {
    this.hoSoApi
      .hoSoCuaToiXetGiao()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) =>
        this.hoSoId.set(ds.find((x) => x.dotId === this.dotId())?.svTrongDotId ?? null),
      );
    this.api
      .deTaiGvDeNghi({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((g) => this.gvs.set(g));
    this.api
      .deTaiBoMon({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((b) => this.boMons.set(b));
    this.tai();
    this.taiNguyenVong();
  }

  protected moTuDeXuat(): void {
    this.formDX.reset({ giangVienDeNghiId: 0, tenDeTai: '', moTa: '', loaiDeTai: 'LyThuyet' });
    this.formDeXuat.set(true);
  }

  protected moDon(): void {
    this.donForm.reset({ loaiDon: 'DoiGVHD', noiDung: '' });
    this.formDon.set(true);
  }

  protected guiNguyenVong(): void {
    const id = this.hoSoId();
    if (!id) return;
    this.dangGui.set(true);
    this.api
      .svNguyenVongDangKy({ id, body: { deTaiIds: this.chon() } })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (ds) => {
          this.dangGui.set(false);
          this.nguyenVong.set(ds);
          this.chon.set([]);
          this.message.success('Đã gửi nguyện vọng. Giảng viên hướng dẫn sẽ xem xét.');
        },
        error: () => this.dangGui.set(false),
      });
  }

  protected guiDeXuat(): void {
    const id = this.hoSoId();
    if (!id || this.formDX.invalid) return;
    const v = this.formDX.getRawValue();
    this.dangGui.set(true);
    this.api
      .deTaiSvDeXuat({
        body: {
          svTrongDotId: id,
          giangVienDeNghiId: v.giangVienDeNghiId,
          tenDeTai: v.tenDeTai.trim(),
          moTa: v.moTa.trim() || null,
          loaiDeTai: v.loaiDeTai as never,
        },
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.dangGui.set(false);
          this.formDeXuat.set(false);
          this.message.success('Đã gửi đề xuất cho giảng viên xác nhận.');
        },
        error: () => this.dangGui.set(false),
      });
  }

  protected guiDon(): void {
    const id = this.hoSoId();
    if (!id || this.donForm.invalid) return;
    const v = this.donForm.getRawValue();
    this.dangGui.set(true);
    this.api
      .svDonDieuChinhTao({ id, body: { loaiDon: v.loaiDon as never, noiDung: v.noiDung.trim() } })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.dangGui.set(false);
          this.formDon.set(false);
          this.message.success('Đã gửi đơn. Trưởng Bộ môn sẽ xem xét.');
        },
        error: () => this.dangGui.set(false),
      });
  }

  protected tai(): void {
    this.dangTai.set(true);
    this.api
      .deTaiDanhSach({ dotId: this.dotId(), boMonId: this.boMonLoc() ?? undefined })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (ds) => {
          this.deTais.set(ds);
          this.dangTai.set(false);
        },
        error: () => this.dangTai.set(false),
      });
  }

  private taiNguyenVong(): void {
    this.hoSoApi
      .hoSoCuaToiXetGiao()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) => {
        const id = ds.find((x) => x.dotId === this.dotId())?.svTrongDotId;
        if (!id) return;
        this.api
          .svNguyenVongLay({ id })
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe((ns) => this.nguyenVong.set(ns));
      });
  }
}
