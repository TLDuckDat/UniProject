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
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';

import type { DonTienDoItem } from '../../api/models/don-tien-do-item';
import type { HoSoTienDoItem } from '../../api/models/ho-so-tien-do-item';
import type { SvCham } from '../../api/models/sv-cham';
import { TienDoService } from '../../api/services/tien-do.service';
import { ngayVn, nhanLoaiDonTienDo, nhanTrangThaiMoc, tenMoc } from './tien-do-nhan';

/**
 * Tab Bộ môn: bảng SV chậm tiến độ (C01/C02), gợi ý RUT khi bỏ 2 mốc (C04 – chỉ gợi ý, không tự động),
 * và duyệt đơn xin dừng (T06 khi duyệt).
 */
@Component({
  selector: 'app-tab-bm-tien-do',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NzAlertModule, NzButtonModule, NzEmptyModule, NzTableModule, NzTagModule],
  template: `
    <nz-alert
      class="mb"
      nzType="info"
      nzShowIcon
      nzMessage="Hệ thống chỉ gợi ý RUT khi bỏ 2 mốc (C04). Quyết định dừng do Bộ môn xem xét và thực hiện qua duyệt đơn xin dừng."
    />

    <h4>Sinh viên chậm tiến độ</h4>
    <nz-table
      #cham
      nzSize="small"
      [nzData]="svCham()"
      [nzShowPagination]="false"
      [nzLoading]="dangTai()"
    >
      <thead>
        <tr>
          <th>Sinh viên</th>
          <th>GVHD</th>
          <th nzAlign="right">Dưới yêu cầu (C02)</th>
          <th nzAlign="right">Quá hạn chưa nộp (C01)</th>
          <th>Gợi ý</th>
        </tr>
      </thead>
      <tbody>
        @for (c of cham.data; track c.svTrongDotId) {
          <tr>
            <td>{{ c.maSV }} – {{ c.hoTen }}</td>
            <td>{{ c.tenGVHD ?? '—' }}</td>
            <td class="so">{{ c.soCanhBao }}</td>
            <td class="so">{{ c.soKhongNop }}</td>
            <td>
              @if (c.canNhacRut) {
                <nz-tag nzColor="red">Xem xét RUT (bỏ 2 mốc)</nz-tag>
              }
            </td>
          </tr>
        } @empty {
          <tr>
            <td colspan="5"><nz-empty nzNotFoundContent="Không có sinh viên chậm tiến độ." /></td>
          </tr>
        }
      </tbody>
    </nz-table>

    <h4>Trạng thái 3 mốc theo sinh viên</h4>
    <nz-table
      #tong
      nzSize="small"
      [nzData]="hoSo()"
      [nzShowPagination]="false"
      [nzLoading]="dangTai()"
    >
      <thead>
        <tr>
          <th>Sinh viên</th>
          <th>Mốc 1</th>
          <th>Mốc 2</th>
          <th>Mốc 3</th>
        </tr>
      </thead>
      <tbody>
        @for (h of tong.data; track h.svTrongDotId) {
          <tr>
            <td>{{ h.maSV }} – {{ h.hoTen }}</td>
            @for (m of h.cacMoc; track m.mocTienDoId) {
              <td>
                <nz-tag [nzColor]="nhan(m.trangThai).color">{{ nhan(m.trangThai).label }}</nz-tag>
              </td>
            }
          </tr>
        }
      </tbody>
    </nz-table>

    <h4>Đơn xin dừng chờ duyệt</h4>
    @for (d of donXinDung(); track d.donTuId) {
      <div class="don">
        <strong>{{ d.maSV }} – {{ d.hoTen }}</strong> · {{ loaiDon(d.loaiDon) }} ({{
          ngay(d.ngayGui)
        }}): {{ d.noiDung }}
        <div>
          <button nz-button nzType="primary" nzSize="small" (click)="duyet(d, true)">
            Duyệt – chuyển RUT
          </button>
          <button nz-button nzDanger nzSize="small" (click)="duyet(d, false)">Từ chối</button>
        </div>
      </div>
    } @empty {
      <p class="phu">Không có đơn xin dừng chờ duyệt.</p>
    }
  `,
  styles: `
    .mb {
      display: block;
      margin-bottom: 12px;
    }
    .so {
      text-align: right;
    }
    .don {
      border: 1px solid #f0f0f0;
      border-radius: 6px;
      padding: 8px 12px;
      margin-bottom: 8px;
    }
    .phu {
      color: rgba(0, 0, 0, 0.55);
    }
    h4 {
      margin: 24px 0 8px;
    }
  `,
})
export class TabBmTienDoComponent implements OnInit {
  readonly dotId = input.required<number>();

  private readonly api = inject(TienDoService);
  private readonly message = inject(NzMessageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly nhan = nhanTrangThaiMoc;
  protected readonly moc = tenMoc;
  protected readonly ngay = ngayVn;
  protected readonly loaiDon = nhanLoaiDonTienDo;

  protected readonly svCham = signal<SvCham[]>([]);
  protected readonly hoSo = signal<HoSoTienDoItem[]>([]);
  protected readonly donXinDung = signal<DonTienDoItem[]>([]);
  protected readonly dangTai = signal(false);

  ngOnInit(): void {
    this.tai();
  }

  protected duyet(d: DonTienDoItem, dongY: boolean): void {
    this.api
      .tienDoDuyetDon({
        id: d.donTuId,
        body: {
          dongY,
          hanMoi: null,
          lyDo: dongY ? 'Đồng ý cho dừng theo đơn.' : 'Không đồng ý cho dừng.',
        },
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.message.success(dongY ? 'Đã duyệt; hồ sơ chuyển RUT.' : 'Đã từ chối đơn.');
        this.tai();
      });
  }

  private tai(): void {
    this.dangTai.set(true);
    this.api
      .tienDoSvCham({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) => this.svCham.set(ds));
    this.api
      .tienDoBoMonDanhSach({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (ds: HoSoTienDoItem[]) => {
          this.hoSo.set(ds);
          this.dangTai.set(false);
        },
        error: () => this.dangTai.set(false),
      });
    this.api
      .tienDoDonDanhSach({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) =>
        this.donXinDung.set(ds.filter((d) => d.loaiDon === 'XinDung' && d.trangThai === 'ChoXuLy')),
      );
  }
}
