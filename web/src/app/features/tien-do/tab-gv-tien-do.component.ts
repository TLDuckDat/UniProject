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
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';

import type { BaoCaoTienDoItem } from '../../api/models/bao-cao-tien-do-item';
import type { DonTienDoItem } from '../../api/models/don-tien-do-item';
import type { HoSoTienDoItem } from '../../api/models/ho-so-tien-do-item';
import { TienDoService } from '../../api/services/tien-do.service';
import {
  ngayVn,
  nhanLoaiDonTienDo,
  nhanTrangThaiDon,
  nhanTrangThaiMoc,
  tenMoc,
} from './tien-do-nhan';

/** Tab GVHD: danh sách SV hướng dẫn với trạng thái 3 mốc (màu), nhận xét báo cáo, duyệt đơn gia hạn. */
@Component({
  selector: 'app-tab-gv-tien-do',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    NzButtonModule,
    NzDrawerModule,
    NzEmptyModule,
    NzInputModule,
    NzInputNumberModule,
    NzModalModule,
    NzTableModule,
    NzTagModule,
  ],
  template: `
    <nz-table
      #bang
      nzSize="middle"
      [nzData]="ds()"
      [nzLoading]="dangTai()"
      [nzShowPagination]="false"
    >
      <thead>
        <tr>
          <th>Sinh viên</th>
          <th>Mốc 1 (tuần 3–4)</th>
          <th>Mốc 2 (tuần 8–9)</th>
          <th>Mốc 3 (tuần 12–13)</th>
          <th>Ghi chú</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        @for (h of bang.data; track h.svTrongDotId) {
          <tr>
            <td>{{ h.maSV }} – {{ h.hoTen }}</td>
            @for (m of h.cacMoc; track m.mocTienDoId) {
              <td>
                <nz-tag [nzColor]="nhan(m.trangThai).color">{{ nhan(m.trangThai).label }}</nz-tag>
                @if (m.baoCaoMoiNhat) {
                  <div class="phu">Nộp {{ ngay(m.baoCaoMoiNhat.ngayNop) }}</div>
                }
              </td>
            }
            <td>
              @if (h.canNhacRut) {
                <nz-tag nzColor="red">Bỏ 2 mốc – gợi ý xem xét RUT</nz-tag>
              }
              @if (h.coDonDangCho) {
                <nz-tag nzColor="orange">Có đơn chờ duyệt</nz-tag>
              }
            </td>
            <td><button nz-button nzType="link" (click)="moChiTiet(h)">Nhận xét</button></td>
          </tr>
        } @empty {
          <tr>
            <td colspan="6">
              <nz-empty nzNotFoundContent="Bạn chưa hướng dẫn sinh viên nào trong đợt này." />
            </td>
          </tr>
        }
      </tbody>
    </nz-table>

    <h4>Đơn gia hạn chờ duyệt</h4>
    @for (d of donGiaHan(); track d.donTuId) {
      <div class="don">
        <strong>{{ d.maSV }} – {{ d.hoTen }}</strong
        >: {{ d.noiDung }}
        <div>
          <button nz-button nzType="primary" nzSize="small" (click)="moDuyetDon(d)">
            Duyệt gia hạn
          </button>
        </div>
      </div>
    } @empty {
      <p class="phu">Không có đơn gia hạn chờ duyệt.</p>
    }

    <nz-drawer
      [nzVisible]="chiTiet() !== null"
      [nzWidth]="560"
      [nzTitle]="'Nhận xét – ' + (chiTiet()?.maSV ?? '')"
      (nzOnClose)="chiTiet.set(null)"
    >
      <ng-container *nzDrawerContent>
        @for (m of chiTiet()?.cacMoc ?? []; track m.mocTienDoId) {
          <div class="moc">
            <strong>{{ moc(m.maMoc) }}</strong>
            <nz-tag [nzColor]="nhan(m.trangThai).color">{{ nhan(m.trangThai).label }}</nz-tag>
            @if (m.baoCaoMoiNhat; as b) {
              <div class="phu">
                Nộp {{ ngay(b.ngayNop) }} · {{ b.tyLeHoanThanhPct }}%
                @if (b.tenFile) {
                  · {{ b.tenFile }}
                }
              </div>
              <button nz-button nzSize="small" (click)="moNhanXet(b)">Nhận xét / chấm điểm</button>
            }
          </div>
        }
      </ng-container>
    </nz-drawer>

    <nz-modal
      [nzVisible]="nhanXetDang() !== null"
      nzTitle="Nhận xét báo cáo tiến độ"
      nzOkText="Lưu nhận xét"
      [nzOkLoading]="dangLuu()"
      (nzOnCancel)="nhanXetDang.set(null)"
      (nzOnOk)="luuNhanXet()"
    >
      <ng-container *nzModalContent>
        <span>Điểm tiến độ (0–10)</span>
        <nz-input-number
          [ngModel]="diem()"
          (ngModelChange)="diem.set($event)"
          [nzMin]="0"
          [nzMax]="10"
          [nzStep]="0.5"
          aria-label="Điểm tiến độ"
        />
        <span class="mt">Nhận xét</span>
        <textarea
          nz-input
          rows="4"
          maxlength="1000"
          [ngModel]="nhanXetText()"
          (ngModelChange)="nhanXetText.set($event)"
          aria-label="Nhận xét"
        ></textarea>
      </ng-container>
    </nz-modal>

    <nz-modal
      [nzVisible]="duyetDang() !== null"
      nzTitle="Duyệt đơn gia hạn"
      nzOkText="Gia hạn"
      [nzOkLoading]="dangLuu()"
      (nzOnCancel)="duyetDang.set(null)"
      (nzOnOk)="duyetGiaHan(true)"
    >
      <ng-container *nzModalContent>
        <p>{{ duyetDang()?.noiDung }}</p>
        <span>Hạn mới – phải sau hạn hiện tại</span>
        <input
          nz-input
          type="date"
          [ngModel]="hanMoi()"
          (ngModelChange)="hanMoi.set($event)"
          aria-label="Hạn mới"
        />
        <div class="mt">
          <button nz-button nzDanger (click)="duyetGiaHan(false)">Từ chối</button>
        </div>
      </ng-container>
    </nz-modal>
  `,
  styles: `
    .phu {
      color: rgba(0, 0, 0, 0.55);
      font-size: 12px;
    }
    .moc {
      border: 1px solid #f0f0f0;
      border-radius: 8px;
      padding: 10px;
      margin-bottom: 10px;
    }
    .don {
      border: 1px solid #f0f0f0;
      border-radius: 6px;
      padding: 8px 12px;
      margin-bottom: 8px;
    }
    .mt {
      margin-top: 12px;
      display: block;
    }
    h4 {
      margin: 24px 0 8px;
    }
  `,
})
export class TabGvTienDoComponent implements OnInit {
  readonly dotId = input.required<number>();

  private readonly api = inject(TienDoService);
  private readonly message = inject(NzMessageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly nhan = nhanTrangThaiMoc;
  protected readonly moc = tenMoc;
  protected readonly ngay = ngayVn;
  protected readonly nhanDon = nhanLoaiDonTienDo;
  protected readonly nhanDonTrangThai = nhanTrangThaiDon;

  protected readonly ds = signal<HoSoTienDoItem[]>([]);
  protected readonly donGiaHan = signal<DonTienDoItem[]>([]);
  protected readonly dangTai = signal(false);
  protected readonly chiTiet = signal<HoSoTienDoItem | null>(null);
  protected readonly nhanXetDang = signal<BaoCaoTienDoItem | null>(null);
  protected readonly diem = signal<number | null>(null);
  protected readonly nhanXetText = signal('');
  protected readonly duyetDang = signal<DonTienDoItem | null>(null);
  protected readonly hanMoi = signal('');
  protected readonly dangLuu = signal(false);

  ngOnInit(): void {
    this.tai();
  }

  protected moChiTiet(h: HoSoTienDoItem): void {
    this.chiTiet.set(h);
  }

  protected moNhanXet(b: BaoCaoTienDoItem): void {
    this.nhanXetDang.set(b);
    this.diem.set(b.diemTienDo ?? null);
    this.nhanXetText.set(b.gvhdNhanXet ?? '');
  }

  protected luuNhanXet(): void {
    const b = this.nhanXetDang();
    if (!b) return;
    this.dangLuu.set(true);
    this.api
      .baoCaoNhanXet({
        id: b.id,
        body: { diemTienDo: this.diem(), nhanXet: this.nhanXetText().trim() || null },
      })
      .subscribe({
        next: () => {
          this.dangLuu.set(false);
          this.nhanXetDang.set(null);
          this.message.success('Đã lưu nhận xét.');
          this.tai();
        },
        error: () => this.dangLuu.set(false),
      });
  }

  protected moDuyetDon(d: DonTienDoItem): void {
    this.hanMoi.set('');
    this.duyetDang.set(d);
  }

  protected duyetGiaHan(dongY: boolean): void {
    const d = this.duyetDang();
    if (!d) return;
    if (dongY && !this.hanMoi()) {
      this.message.warning('Cần nhập hạn mới.');
      return;
    }
    this.dangLuu.set(true);
    this.api
      .tienDoDuyetDon({
        id: d.donTuId,
        body: {
          dongY,
          hanMoi: dongY ? this.hanMoi() : null,
          lyDo: dongY ? null : 'Không đồng ý gia hạn.',
        },
      })
      .subscribe({
        next: () => {
          this.dangLuu.set(false);
          this.duyetDang.set(null);
          this.message.success('Đã xử lý đơn.');
          this.tai();
        },
        error: () => this.dangLuu.set(false),
      });
  }

  private tai(): void {
    this.dangTai.set(true);
    this.api
      .tienDoGvDanhSach({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (ds) => {
          this.ds.set(ds);
          this.dangTai.set(false);
        },
        error: () => this.dangTai.set(false),
      });
    this.api
      .tienDoDonDanhSach({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) =>
        this.donGiaHan.set(ds.filter((d) => d.loaiDon === 'GiaHan' && d.trangThai === 'ChoXuLy')),
      );
  }
}
