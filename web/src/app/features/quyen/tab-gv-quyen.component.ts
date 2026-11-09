import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnDestroy,
  OnInit,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzRadioModule } from 'ng-zorro-antd/radio';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';

import type { HoSoTienDoItem } from '../../api/models/ho-so-tien-do-item';
import type { QuyenCanDocItem } from '../../api/models/quyen-can-doc-item';
import { QuyenDocDuyetService } from '../../api/services/quyen-doc-duyet.service';
import { TienDoService } from '../../api/services/tien-do.service';
import { mauHanDoc, nhanHanDoc, nhanKetQuaDocDuyet, nhanLoaiBan } from './quyen-nhan';
import { QuyenTaiService } from './quyen-tai.service';

/** Tab GV: GVHD ghi ý kiến (D05); GVĐD có hàng đợi quyển cần đọc với đếm ngược, xem PDF, kết luận 3 lựa chọn (D06, D08, D10). */
@Component({
  selector: 'app-tab-gv-quyen',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    FormsModule,
    NzAlertModule,
    NzButtonModule,
    NzDrawerModule,
    NzEmptyModule,
    NzInputModule,
    NzInputNumberModule,
    NzModalModule,
    NzRadioModule,
    NzTableModule,
    NzTagModule,
  ],
  template: `
    <h4>Hàng đợi quyển cần đọc (GVĐD)</h4>
    <nz-table
      nzSize="middle"
      [nzData]="canDoc()"
      [nzShowPagination]="false"
      [nzLoading]="dangTai()"
    >
      <thead>
        <tr>
          <th>Sinh viên</th>
          <th>Bản</th>
          <th>Nộp</th>
          <th>Hạn đọc</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        @for (d of canDoc(); track d.phienBanQuyenId) {
          <tr>
            <td>{{ d.maSV }} – {{ d.hoTen }}</td>
            <td>v{{ d.soPhienBan }} · {{ loai(d.loaiBan) }} · vòng {{ d.vongDoc }}</td>
            <td>{{ d.ngayNop | date: 'dd/MM/yyyy HH:mm' }}</td>
            <td>
              <nz-tag [nzColor]="mau(d.conLaiNgay)">{{ han(d.conLaiNgay) }}</nz-tag>
            </td>
            <td><button nz-button nzType="link" (click)="moDoc(d)">Đọc và kết luận</button></td>
          </tr>
        } @empty {
          <tr>
            <td colspan="5"><nz-empty nzNotFoundContent="Không có quyển cần đọc." /></td>
          </tr>
        }
      </tbody>
    </nz-table>

    <h4>Sinh viên hướng dẫn – ý kiến cho bảo vệ (GVHD)</h4>
    <nz-table nzSize="middle" [nzData]="huongDan()" [nzShowPagination]="false">
      <thead>
        <tr>
          <th>Sinh viên</th>
          <th>Trạng thái hồ sơ</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        @for (h of huongDan(); track h.svTrongDotId) {
          <tr>
            <td>{{ h.maSV }} – {{ h.hoTen }}</td>
            <td>{{ h.trangThaiHoSo }}</td>
            <td><button nz-button nzType="link" (click)="moYKien(h)">Ghi ý kiến</button></td>
          </tr>
        } @empty {
          <tr>
            <td colspan="3"><nz-empty nzNotFoundContent="Bạn chưa hướng dẫn sinh viên nào." /></td>
          </tr>
        }
      </tbody>
    </nz-table>

    <nz-drawer
      [nzVisible]="docDang() !== null"
      [nzWidth]="860"
      [nzTitle]="'Đọc quyển – ' + (docDang()?.maSV ?? '')"
      (nzOnClose)="dongDoc()"
    >
      <ng-container *nzDrawerContent>
        @if (pdfUrl(); as url) {
          <iframe class="pdf" [src]="url" title="Bản quyển"></iframe>
        } @else {
          <nz-alert nzType="info" nzShowIcon nzMessage="Đang tải bản quyển…" />
        }

        <h4>Kết luận</h4>
        <nz-radio-group [(ngModel)]="ketQua" aria-label="Kết luận đọc duyệt">
          <!-- eslint-disable-next-line @angular-eslint/template/label-has-associated-control -- NG-ZORRO dựng <input> bên trong label -->
          <label nz-radio nzValue="DongY">Đồng ý cho bảo vệ</label>
          <!-- eslint-disable-next-line @angular-eslint/template/label-has-associated-control -- NG-ZORRO dựng <input> bên trong label -->
          <label nz-radio nzValue="SuaGap">Yêu cầu sửa gấp (lỗi nhỏ, vòng 1)</label>
          <!-- eslint-disable-next-line @angular-eslint/template/label-has-associated-control -- NG-ZORRO dựng <input> bên trong label -->
          <label nz-radio nzValue="KhongDongY">Không đồng ý cho bảo vệ</label>
        </nz-radio-group>

        @if (ketQua === 'DongY') {
          <div class="dong">
            <span>Điểm GVĐD (bắt buộc khi đồng ý)</span>
            <nz-input-number
              [(ngModel)]="diem"
              [nzMin]="0"
              [nzMax]="10"
              [nzStep]="0.5"
              aria-label="Điểm GVĐD"
            />
          </div>
        }
        @if (ketQua === 'SuaGap') {
          <div class="dong">
            <span>Hạn sửa (yyyy-MM-dd, 1–3 ngày)</span>
            <input nz-input type="date" [(ngModel)]="hanSua" aria-label="Hạn sửa" />
          </div>
        }
        <div class="dong">
          <span>Nhận xét {{ ketQua === 'KhongDongY' ? '(bắt buộc)' : '' }}</span>
          <textarea
            nz-input
            rows="3"
            maxlength="2000"
            [(ngModel)]="nhanXet"
            aria-label="Nhận xét"
          ></textarea>
        </div>
        <button
          nz-button
          nzType="primary"
          [nzLoading]="dangLuu()"
          [disabled]="!hopLe()"
          (click)="gui()"
        >
          Ghi kết luận
        </button>
      </ng-container>
    </nz-drawer>

    <nz-modal
      [nzVisible]="yKienDang() !== null"
      [nzTitle]="'Ý kiến GVHD – ' + (yKienDang()?.maSV ?? '')"
      nzOkText="Ghi ý kiến"
      [nzOkLoading]="dangLuu()"
      (nzOnCancel)="yKienDang.set(null)"
      (nzOnOk)="guiYKien()"
    >
      <ng-container *nzModalContent>
        <nz-radio-group [(ngModel)]="dongYGvhd" aria-label="Đồng ý cho bảo vệ">
          <!-- eslint-disable-next-line @angular-eslint/template/label-has-associated-control -- NG-ZORRO dựng <input> bên trong label -->
          <label nz-radio [nzValue]="true">Đồng ý cho bảo vệ</label>
          <!-- eslint-disable-next-line @angular-eslint/template/label-has-associated-control -- NG-ZORRO dựng <input> bên trong label -->
          <label nz-radio [nzValue]="false">Không đồng ý</label>
        </nz-radio-group>
        @if (!dongYGvhd) {
          <div class="dong">
            <span>Lý do (bắt buộc – D05)</span>
            <textarea
              nz-input
              rows="3"
              maxlength="500"
              [(ngModel)]="lyDoGvhd"
              aria-label="Lý do"
            ></textarea>
          </div>
        }
        <div class="dong">
          <span>Điểm GVHD (0–10, tùy chọn)</span>
          <nz-input-number
            [(ngModel)]="diemGvhd"
            [nzMin]="0"
            [nzMax]="10"
            [nzStep]="0.5"
            aria-label="Điểm GVHD"
          />
        </div>
      </ng-container>
    </nz-modal>
  `,
  styles: `
    h4 {
      margin: 20px 0 8px;
    }
    .pdf {
      width: 100%;
      height: 60vh;
      border: 1px solid #f0f0f0;
      border-radius: 6px;
    }
    .dong {
      margin: 12px 0;
    }
    .dong span {
      display: block;
      margin-bottom: 4px;
    }
  `,
})
export class TabGvQuyenComponent implements OnInit, OnDestroy {
  readonly dotId = input.required<number>();

  private readonly api = inject(QuyenDocDuyetService);
  private readonly tienDo = inject(TienDoService);
  private readonly taiQuyen = inject(QuyenTaiService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly message = inject(NzMessageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loai = nhanLoaiBan;
  protected readonly mau = mauHanDoc;
  protected readonly han = nhanHanDoc;
  protected readonly ketQuaNhan = nhanKetQuaDocDuyet;

  protected readonly canDoc = signal<QuyenCanDocItem[]>([]);
  protected readonly huongDan = signal<HoSoTienDoItem[]>([]);
  protected readonly dangTai = signal(false);
  protected readonly dangLuu = signal(false);

  // Đọc duyệt (GVĐD)
  protected readonly docDang = signal<QuyenCanDocItem | null>(null);
  protected readonly pdfUrl = signal<SafeResourceUrl | null>(null);
  private blobUrl: string | null = null;
  protected ketQua: 'DongY' | 'SuaGap' | 'KhongDongY' = 'DongY';
  protected diem: number | null = null;
  protected hanSua = '';
  protected nhanXet = '';

  // Ý kiến (GVHD)
  protected readonly yKienDang = signal<HoSoTienDoItem | null>(null);
  protected dongYGvhd = true;
  protected lyDoGvhd = '';
  protected diemGvhd: number | null = null;

  ngOnInit(): void {
    this.tai();
  }

  ngOnDestroy(): void {
    this.giaiPhongBlob();
  }

  protected hopLe(): boolean {
    if (this.ketQua === 'DongY') return this.diem !== null;
    if (this.ketQua === 'SuaGap') return !!this.hanSua;
    return this.nhanXet.trim().length > 0;
  }

  protected moDoc(d: QuyenCanDocItem): void {
    this.docDang.set(d);
    this.ketQua = 'DongY';
    this.diem = null;
    this.hanSua = '';
    this.nhanXet = '';
    this.giaiPhongBlob();
    this.taiQuyen
      .taiPhienBan(d.phienBanQuyenId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((blob) => {
        this.blobUrl = URL.createObjectURL(blob);
        this.pdfUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(this.blobUrl));
      });
  }

  protected dongDoc(): void {
    this.docDang.set(null);
    this.giaiPhongBlob();
  }

  protected gui(): void {
    const d = this.docDang();
    if (!d || !this.hopLe()) return;
    this.dangLuu.set(true);
    this.api
      .quyenDocDuyet({
        id: d.svTrongDotId,
        body: {
          ketQua: this.ketQua as never,
          nhanXet: this.nhanXet.trim() || null,
          diemGVDD: this.ketQua === 'DongY' ? this.diem : null,
          hanSuaDen: this.ketQua === 'SuaGap' ? (this.hanSua as never) : null,
        },
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.dangLuu.set(false);
          this.message.success(
            `Đã ghi kết luận: ${this.ketQuaNhan(this.ketQua).label.toLowerCase()}.`,
          );
          this.dongDoc();
          this.tai();
        },
        error: () => this.dangLuu.set(false),
      });
  }

  protected moYKien(h: HoSoTienDoItem): void {
    this.dongYGvhd = true;
    this.lyDoGvhd = '';
    this.diemGvhd = null;
    this.yKienDang.set(h);
  }

  protected guiYKien(): void {
    const h = this.yKienDang();
    if (!h) return;
    if (!this.dongYGvhd && !this.lyDoGvhd.trim()) {
      this.message.warning('Không đồng ý phải ghi lý do.');
      return;
    }
    this.dangLuu.set(true);
    this.api
      .quyenYKienGvhd({
        id: h.svTrongDotId,
        body: {
          dongY: this.dongYGvhd,
          lyDoKhongDongY: this.dongYGvhd ? null : this.lyDoGvhd.trim(),
          diemGVHD: this.diemGvhd,
          diemKhac: null,
        },
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.dangLuu.set(false);
          this.yKienDang.set(null);
          this.message.success('Đã ghi ý kiến hướng dẫn.');
          this.tai();
        },
        error: () => this.dangLuu.set(false),
      });
  }

  private giaiPhongBlob(): void {
    if (this.blobUrl) {
      URL.revokeObjectURL(this.blobUrl);
      this.blobUrl = null;
    }
    this.pdfUrl.set(null);
  }

  private tai(): void {
    this.dangTai.set(true);
    this.api
      .quyenCanDoc({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (ds) => {
          this.canDoc.set(ds);
          this.dangTai.set(false);
        },
        error: () => this.dangTai.set(false),
      });
    this.tienDo
      .tienDoGvDanhSach({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) => this.huongDan.set(ds));
  }
}
