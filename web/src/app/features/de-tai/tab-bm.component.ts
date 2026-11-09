import { DatePipe } from '@angular/common';
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
  FormControl,
  FormsModule,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';

import type { DeTaiItem } from '../../api/models/de-tai-item';
import type { DonDieuChinhItem } from '../../api/models/don-dieu-chinh-item';
import type { GoiYPhanCongItem } from '../../api/models/goi-y-phan-cong-item';
import type { PhanCongHoSoItem } from '../../api/models/phan-cong-ho-so-item';
import type { PhanCongLichSuItem } from '../../api/models/phan-cong-lich-su-item';
import type { TaiGiangVienItem } from '../../api/models/tai-giang-vien-item';
import { DeTaiService } from '../../api/services/de-tai.service';
import {
  LOAI_DE_TAI_LUA_CHON,
  nhanLoaiDeTai,
  nhanLoaiDon,
  nhanMucTai,
  nhanVaiTro,
  tyLeTai,
} from './de-tai-nhan';

/** Lựa chọn phân công nháp của một hồ sơ (chưa ghi cho đến khi bấm Xác nhận). */
interface LuaChonPhanCong {
  gvhd: number | null;
  gvdd: number | null;
  dongHd: number[];
  vuotQuota: boolean;
}

/**
 * Tab Trưởng BM: duyệt đề tài (B01–B02, B04), bảng tải GV x/quota màu cảnh báo (B03),
 * phân công GVHD/GVĐD/đồng HD (B05–B07, B11) với gợi ý tự động chỉ để xem,
 * duyệt đơn đổi GVHD / đổi đề tài sau QĐ (B08, B09) và lịch sử phân công.
 */
@Component({
  selector: 'app-tab-bm',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    FormsModule,
    ReactiveFormsModule,
    NzAlertModule,
    NzButtonModule,
    NzCheckboxModule,
    NzDrawerModule,
    NzEmptyModule,
    NzFormModule,
    NzInputModule,
    NzModalModule,
    NzProgressModule,
    NzSelectModule,
    NzTableModule,
    NzTagModule,
  ],
  template: `
    <h4>1. Duyệt đề tài</h4>
    <nz-table
      #duyetBang
      nzSize="small"
      [nzData]="choDuyet()"
      [nzShowPagination]="false"
      [nzLoading]="dangTai()"
    >
      <thead>
        <tr>
          <th>Đề tài</th>
          <th>Bộ môn</th>
          <th>Đề xuất bởi</th>
          <th>Loại</th>
          <th nzAlign="right">SV tối đa</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        @for (d of duyetBang.data; track d.id) {
          <tr>
            <td>
              {{ d.tenDeTai }}
              @if (d.canhBaoTrung) {
                <nz-tag nzColor="gold">Trùng năm trước</nz-tag>
              }
            </td>
            <td>{{ d.maBoMon }}</td>
            <td>{{ d.tenGVDeXuat ?? d.maSVDeXuat ?? '—' }}</td>
            <td>{{ loai(d.loaiDeTai) }}</td>
            <td class="so">{{ d.maxSV }}</td>
            <td class="hanh-dong">
              <button nz-button nzType="link" (click)="duyet(d, true)">Duyệt</button>
              <button nz-button nzType="link" nzDanger (click)="duyet(d, false)">Từ chối</button>
            </td>
          </tr>
        } @empty {
          <tr>
            <td colspan="6"><nz-empty nzNotFoundContent="Không có đề tài chờ duyệt." /></td>
          </tr>
        }
      </tbody>
    </nz-table>

    <h4>2. Tải giảng viên trong đợt</h4>
    <div class="the-gv">
      @for (g of tai(); track g.giangVienId) {
        <div class="the" [class.vuot]="g.mucTai === 'VuotQuota'">
          <div class="ten">{{ g.maGV }} – {{ g.hoTen }}</div>
          <div class="phu">
            {{
              g.loaiGV === 'NgoaiTruong'
                ? 'Ngoài trường'
                : g.loaiGV === 'CoHuu'
                  ? 'Cơ hữu'
                  : 'Thỉnh giảng'
            }}
          </div>
          <div class="so-luong">
            <strong>{{ g.dangCo }}/{{ g.quota }}</strong> SV
            <nz-tag [nzColor]="mucTai(g.mucTai).color">{{ mucTai(g.mucTai).label }}</nz-tag>
          </div>
          <nz-progress
            [nzPercent]="tyLe(g.dangCo, g.quota)"
            [nzShowInfo]="false"
            [nzStatus]="g.mucTai === 'VuotQuota' ? 'exception' : 'normal'"
          />
        </div>
      } @empty {
        <nz-empty nzNotFoundContent="Chưa có giảng viên trong phạm vi bộ môn." />
      }
    </div>

    <h4>3. Phân công GVHD / GVĐD</h4>
    <div class="thanh">
      <button nz-button (click)="goiY()" [nzLoading]="dangGoiY()">Gợi ý theo tải GV</button>
      <span class="goi-y">Gợi ý chỉ hiển thị để bạn xem; chỉ “Xác nhận” mới ghi phân công.</span>
    </div>
    <nz-table
      #phanCongBang
      nzSize="small"
      [nzData]="hoSo()"
      [nzShowPagination]="false"
      [nzLoading]="dangTai()"
    >
      <thead>
        <tr>
          <th>Mã SV</th>
          <th>Họ tên</th>
          <th>Đề tài</th>
          <th>GVHD</th>
          <th>GVĐD</th>
          <th>Đồng HD</th>
          <th>Thao tác</th>
        </tr>
      </thead>
      <tbody>
        @for (h of phanCongBang.data; track h.svTrongDotId) {
          <tr>
            <td>{{ h.maSV }}</td>
            <td>{{ h.hoTen }}</td>
            <td>{{ h.tenDeTai ?? '— chưa có —' }}</td>
            <td>
              @if (h.gvhd) {
                {{ h.gvhd.maGV }}
              } @else {
                <nz-select
                  class="o-chon"
                  nzShowSearch
                  nzPlaceHolder="GVHD"
                  [ngModel]="luaChon(h.svTrongDotId).gvhd"
                  (ngModelChange)="capNhat(h.svTrongDotId, 'gvhd', $event)"
                >
                  @for (g of tai(); track g.giangVienId) {
                    <nz-option
                      [nzValue]="g.giangVienId"
                      [nzLabel]="g.maGV + ' (' + g.dangCo + '/' + g.quota + ')'"
                    />
                  }
                </nz-select>
              }
            </td>
            <td>
              @if (h.gvdd) {
                {{ h.gvdd.maGV }}
              } @else if (h.gvhd) {
                —
              } @else {
                <nz-select
                  class="o-chon"
                  nzAllowClear
                  nzPlaceHolder="GVĐD"
                  [ngModel]="luaChon(h.svTrongDotId).gvdd"
                  (ngModelChange)="capNhat(h.svTrongDotId, 'gvdd', $event)"
                >
                  @for (g of tai(); track g.giangVienId) {
                    <nz-option
                      [nzValue]="g.giangVienId"
                      [nzLabel]="g.maGV + ' (' + g.dangCo + '/' + g.quota + ')'"
                    />
                  }
                </nz-select>
              }
            </td>
            <td>
              @if (h.dongHD.length > 0) {
                {{ tenDanhSach(h.dongHD) }}
              } @else if (!h.gvhd) {
                <nz-select
                  class="o-chon"
                  nzMode="multiple"
                  nzPlaceHolder="Đồng HD"
                  [ngModel]="luaChon(h.svTrongDotId).dongHd"
                  (ngModelChange)="capNhat(h.svTrongDotId, 'dongHd', $event)"
                >
                  @for (g of tai(); track g.giangVienId) {
                    <nz-option [nzValue]="g.giangVienId" [nzLabel]="g.maGV" />
                  }
                </nz-select>
              }
            </td>
            <td class="hanh-dong">
              @if (!h.gvhd) {
                <!-- eslint-disable-next-line @angular-eslint/template/label-has-associated-control -- NG-ZORRO dựng <input> bên trong label -->
                <label
                  nz-checkbox
                  [ngModel]="luaChon(h.svTrongDotId).vuotQuota"
                  (ngModelChange)="capNhat(h.svTrongDotId, 'vuotQuota', $event)"
                >
                  Duyệt ngoại lệ quota
                </label>
                <button
                  nz-button
                  nzType="primary"
                  nzSize="small"
                  [disabled]="!luaChon(h.svTrongDotId).gvhd"
                  (click)="xacNhan(h)"
                >
                  Xác nhận
                </button>
              }
              <button nz-button nzType="link" (click)="moLichSu(h)">Lịch sử</button>
            </td>
          </tr>
        } @empty {
          <tr>
            <td colspan="7"><nz-empty nzNotFoundContent="Không có sinh viên trong phạm vi." /></td>
          </tr>
        }
      </tbody>
    </nz-table>

    <h4>4. Đơn đổi GVHD / đổi đề tài</h4>
    <nz-table
      #donBang
      nzSize="small"
      [nzData]="don()"
      [nzShowPagination]="false"
      [nzLoading]="dangTai()"
    >
      <thead>
        <tr>
          <th>Sinh viên</th>
          <th>Loại đơn</th>
          <th>Nội dung</th>
          <th>Hiện tại</th>
          <th>Trạng thái</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        @for (d of donBang.data; track d.donTuId) {
          <tr>
            <td>{{ d.maSV }} – {{ d.hoTen }}</td>
            <td>{{ nhanDon(d.loaiDon) }}</td>
            <td class="noi-dung">{{ d.noiDung }}</td>
            <td>{{ d.gvhdHienTai ? 'GVHD: ' + d.gvhdHienTai : (d.tenDeTai ?? '') }}</td>
            <td>
              @if (d.trangThai === 'ChoXuLy') {
                <nz-tag nzColor="orange">Chờ duyệt</nz-tag>
              } @else if (d.trangThai === 'DaDuyet') {
                <nz-tag nzColor="green">Đã duyệt</nz-tag>
              } @else {
                <nz-tag nzColor="red">Từ chối</nz-tag>
              }
            </td>
            <td class="hanh-dong">
              @if (d.trangThai === 'ChoXuLy') {
                <button nz-button nzType="link" (click)="moDuyetDon(d)">Xử lý</button>
              } @else {
                <span>{{ d.ketQuaXuLy }}</span>
              }
            </td>
          </tr>
        } @empty {
          <tr>
            <td colspan="6"><nz-empty nzNotFoundContent="Không có đơn đổi GVHD / đề tài." /></td>
          </tr>
        }
      </tbody>
    </nz-table>

    <nz-modal
      [nzVisible]="donDangXu() !== null"
      nzTitle="Xử lý đơn điều chỉnh"
      [nzOkText]="xuLyDongY() ? 'Duyệt và ghi quyết định' : 'Từ chối'"
      [nzOkDanger]="!xuLyDongY()"
      [nzOkDisabled]="!hopLeXuLy()"
      [nzOkLoading]="dangXuLy()"
      (nzOnCancel)="donDangXu.set(null)"
      (nzOnOk)="xuLyDon()"
    >
      <ng-container *nzModalContent>
        @if (donDangXu(); as d) {
          <p>
            <strong>{{ d.maSV }} – {{ d.hoTen }}</strong> · {{ nhanDon(d.loaiDon) }}
          </p>
          <blockquote>{{ d.noiDung }}</blockquote>
          <div class="chon-quyet-dinh">
            <nz-select
              [ngModel]="xuLyDongY()"
              (ngModelChange)="xuLyDongY.set($event)"
              aria-label="Quyết định"
            >
              <nz-option [nzValue]="true" nzLabel="Chấp nhận" />
              <nz-option [nzValue]="false" nzLabel="Từ chối" />
            </nz-select>
          </div>
          @if (xuLyDongY()) {
            <form nz-form nzLayout="vertical" [formGroup]="formXuLy">
              <nz-form-item>
                <nz-form-label nzRequired>Số quyết định điều chỉnh</nz-form-label>
                <nz-form-control
                  ><input nz-input formControlName="soQuyetDinh" maxlength="50"
                /></nz-form-control>
              </nz-form-item>
              <nz-form-item>
                <nz-form-label nzRequired>Ngày ký</nz-form-label>
                <nz-form-control
                  ><input nz-input type="date" formControlName="ngayKy"
                /></nz-form-control>
              </nz-form-item>
              @if (d.loaiDon === 'DoiGVHD') {
                <nz-form-item>
                  <nz-form-label nzRequired>GVHD mới</nz-form-label>
                  <nz-form-control>
                    <nz-select
                      formControlName="giangVienMoiId"
                      nzShowSearch
                      nzPlaceHolder="Chọn GVHD mới"
                    >
                      @for (g of tai(); track g.giangVienId) {
                        <nz-option
                          [nzValue]="g.giangVienId"
                          [nzLabel]="g.maGV + ' (' + g.dangCo + '/' + g.quota + ')'"
                        />
                      }
                    </nz-select>
                  </nz-form-control>
                </nz-form-item>
              } @else {
                <nz-form-item>
                  <nz-form-label nzRequired>Tên đề tài mới</nz-form-label>
                  <nz-form-control
                    ><input nz-input formControlName="tenDeTaiMoi" maxlength="500"
                  /></nz-form-control>
                </nz-form-item>
              }
            </form>
          } @else {
            <textarea
              nz-input
              rows="3"
              [ngModel]="lyDoTuChoi()"
              (ngModelChange)="lyDoTuChoi.set($event)"
              aria-label="Lý do từ chối"
            ></textarea>
          }
        }
      </ng-container>
    </nz-modal>

    <nz-drawer
      [nzVisible]="lichSu() !== null"
      [nzWidth]="560"
      nzTitle="Lịch sử phân công"
      (nzOnClose)="lichSu.set(null)"
    >
      <ng-container *nzDrawerContent>
        <nz-table nzSize="small" [nzData]="lichSuDs()" [nzShowPagination]="false">
          <thead>
            <tr>
              <th>Vai trò</th>
              <th>Giảng viên</th>
              <th>Từ</th>
              <th>Đến</th>
              <th>Ghi chú</th>
            </tr>
          </thead>
          <tbody>
            @for (x of lichSuDs(); track x.id) {
              <tr>
                <td>{{ vaiTro(x.vaiTro) }}</td>
                <td>{{ x.maGV }} – {{ x.hoTen }}</td>
                <td>{{ x.ngayBatDau | date: 'dd/MM/yyyy' }}</td>
                <td>{{ x.ngayKetThuc ? (x.ngayKetThuc | date: 'dd/MM/yyyy') : 'Hiện hành' }}</td>
                <td>
                  {{ x.lyDoThayDoi ?? '' }} {{ x.soQuyetDinh ? '(QĐ ' + x.soQuyetDinh + ')' : ''
                  }}{{ x.vuotQuotaDuyetBoi ? ' · duyệt vượt quota' : '' }}
                </td>
              </tr>
            }
          </tbody>
        </nz-table>
      </ng-container>
    </nz-drawer>
  `,
  styles: `
    h4 {
      margin: 24px 0 8px;
    }
    .so {
      text-align: right;
    }
    .thanh {
      display: flex;
      gap: 12px;
      align-items: center;
      margin-bottom: 8px;
    }
    .goi-y {
      color: rgba(0, 0, 0, 0.55);
      font-size: 13px;
    }
    .the-gv {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
      gap: 12px;
    }
    .the {
      border: 1px solid #f0f0f0;
      border-radius: 8px;
      padding: 12px;
    }
    .the.vuot {
      border-color: #ffa39e;
      background: #fff1f0;
    }
    .ten {
      font-weight: 600;
    }
    .phu {
      color: rgba(0, 0, 0, 0.55);
      font-size: 12px;
    }
    .so-luong {
      margin: 6px 0;
    }
    .o-chon {
      min-width: 160px;
    }
    .hanh-dong {
      white-space: nowrap;
    }
    .hanh-dong a,
    .hanh-dong button {
      margin-right: 8px;
    }
    .noi-dung {
      max-width: 320px;
      white-space: pre-line;
    }
    .chon-quyet-dinh {
      margin: 8px 0 12px;
    }
  `,
})
export class TabBmComponent implements OnInit {
  readonly dotId = input.required<number>();

  private readonly api = inject(DeTaiService);
  private readonly message = inject(NzMessageService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly loai = nhanLoaiDeTai;
  protected readonly mucTai = nhanMucTai;
  protected readonly nhanDon = nhanLoaiDon;
  protected readonly vaiTro = nhanVaiTro;
  protected readonly tyLe = tyLeTai;
  protected readonly loaiLuaChon = LOAI_DE_TAI_LUA_CHON;

  protected readonly dangTai = signal(false);
  protected readonly deTais = signal<DeTaiItem[]>([]);
  protected readonly choDuyet = computed(() =>
    this.deTais().filter((d) => d.trangThaiDuyet === 'ChoDuyet'),
  );
  protected readonly tai = signal<TaiGiangVienItem[]>([]);
  protected readonly hoSo = signal<PhanCongHoSoItem[]>([]);
  protected readonly don = signal<DonDieuChinhItem[]>([]);
  protected readonly luaChonMap = signal<Record<number, LuaChonPhanCong>>({});
  protected readonly dangGoiY = signal(false);
  protected readonly lichSu = signal<PhanCongHoSoItem | null>(null);
  protected readonly lichSuDs = signal<PhanCongLichSuItem[]>([]);

  // ----- xử lý đơn
  protected readonly donDangXu = signal<DonDieuChinhItem | null>(null);
  protected readonly xuLyDongY = signal(true);
  protected readonly lyDoTuChoi = signal('');
  protected readonly dangXuLy = signal(false);
  protected readonly formXuLy = this.fb.group({
    soQuyetDinh: ['', [Validators.required, Validators.maxLength(50)]],
    ngayKy: ['', Validators.required],
    giangVienMoiId: new FormControl<number | null>(null),
    tenDeTaiMoi: ['', Validators.maxLength(500)],
  });
  /** Kiểm tra theo giá trị form hiện tại (không dùng computed vì FormGroup không phải signal). */
  protected hopLeXuLy(): boolean {
    const d = this.donDangXu();
    if (!d) return false;
    if (!this.xuLyDongY()) return this.lyDoTuChoi().trim().length > 0;
    const v = this.formXuLy.getRawValue();
    if (!v.soQuyetDinh.trim() || !v.ngayKy) return false;
    return d.loaiDon === 'DoiGVHD' ? v.giangVienMoiId != null : v.tenDeTaiMoi.trim().length > 0;
  }

  ngOnInit(): void {
    this.nap();
  }

  protected duyet(d: DeTaiItem, dongY: boolean): void {
    this.api
      .deTaiBoMonDuyet({ id: d.id, body: { dongY } })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.message.success(dongY ? 'Đã duyệt đề tài.' : 'Đã từ chối đề tài.');
        this.nap();
      });
  }

  protected luaChon(id: number): LuaChonPhanCong {
    return this.luaChonMap()[id] ?? { gvhd: null, gvdd: null, dongHd: [], vuotQuota: false };
  }

  protected capNhat(id: number, khoa: keyof LuaChonPhanCong, gia: unknown): void {
    const cu = this.luaChon(id);
    this.luaChonMap.set({ ...this.luaChonMap(), [id]: { ...cu, [khoa]: gia as never } });
  }

  /** Gợi ý chỉ điền vào lựa chọn nháp; không ghi dữ liệu (B05). */
  protected goiY(): void {
    this.dangGoiY.set(true);
    this.api
      .deTaiGoiYPhanCong({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (gy: GoiYPhanCongItem[]) => {
          const map = { ...this.luaChonMap() };
          for (const g of gy) {
            if (g.goiYGVHDId == null) continue;
            map[g.svTrongDotId] = { gvhd: g.goiYGVHDId, gvdd: null, dongHd: [], vuotQuota: false };
          }
          this.luaChonMap.set(map);
          this.dangGoiY.set(false);
          this.message.info(
            `Đã gợi ý GVHD cho ${gy.filter((g) => g.goiYGVHDId != null).length} sinh viên (chưa ghi).`,
          );
        },
        error: () => this.dangGoiY.set(false),
      });
  }

  protected xacNhan(h: PhanCongHoSoItem): void {
    const l = this.luaChon(h.svTrongDotId);
    if (l.gvhd == null) return;
    this.api
      .deTaiPhanCong({
        body: {
          svTrongDotId: h.svTrongDotId,
          gvhdId: l.gvhd,
          gvddId: l.gvdd,
          dongHDIds: l.dongHd,
          deTaiId: h.deTaiId ?? null,
          vuotQuotaXacNhan: l.vuotQuota,
        },
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.message.success(`Đã phân công cho ${h.maSV}.`);
        const map = { ...this.luaChonMap() };
        delete map[h.svTrongDotId];
        this.luaChonMap.set(map);
        this.nap();
      });
  }

  protected tenDanhSach(ds: { maGV: string }[]): string {
    return ds.map((x) => x.maGV).join(', ');
  }

  protected moLichSu(h: PhanCongHoSoItem): void {
    this.lichSu.set(h);
    this.api
      .svLichSuPhanCong({ id: h.svTrongDotId })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) => this.lichSuDs.set(ds));
  }

  protected moDuyetDon(d: DonDieuChinhItem): void {
    this.xuLyDongY.set(true);
    this.lyDoTuChoi.set('');
    this.formXuLy.reset({
      soQuyetDinh: '',
      ngayKy: new Date().toISOString().slice(0, 10),
      giangVienMoiId: null,
      tenDeTaiMoi: '',
    });
    this.donDangXu.set(d);
  }

  protected xuLyDon(): void {
    const d = this.donDangXu();
    if (!d || !this.hopLeXuLy()) {
      this.message.warning('Cần điền đủ thông tin quyết định (hoặc lý do từ chối).');
      return;
    }
    const v = this.formXuLy.getRawValue();
    this.dangXuLy.set(true);
    this.api
      .deTaiDuyetDonDieuChinh({
        id: d.donTuId,
        body: this.xuLyDongY()
          ? {
              dongY: true,
              soQuyetDinh: v.soQuyetDinh.trim(),
              ngayKy: v.ngayKy,
              giangVienMoiId: d.loaiDon === 'DoiGVHD' ? v.giangVienMoiId : null,
              tenDeTaiMoi: d.loaiDon === 'DoiDeTai' ? v.tenDeTaiMoi.trim() : null,
              moTaMoi: null,
              lyDo: null,
            }
          : {
              dongY: false,
              lyDo: this.lyDoTuChoi().trim(),
              soQuyetDinh: null,
              ngayKy: null,
              giangVienMoiId: null,
              tenDeTaiMoi: null,
              moTaMoi: null,
            },
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.dangXuLy.set(false);
          this.donDangXu.set(null);
          this.message.success('Đã xử lý đơn.');
          this.nap();
        },
        error: () => this.dangXuLy.set(false),
      });
  }

  private nap(): void {
    this.dangTai.set(true);
    this.api
      .deTaiDanhSach({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) => this.deTais.set(ds));
    this.api
      .deTaiPhanCongDot({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.hoSo.set(res.hoSo);
          this.tai.set(res.tai);
          this.dangTai.set(false);
        },
        error: () => this.dangTai.set(false),
      });
    this.api
      .deTaiDonDieuChinhDanhSach({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) => this.don.set(ds));
  }
}
