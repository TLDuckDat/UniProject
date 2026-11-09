import { DatePipe, DecimalPipe } from '@angular/common';
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
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTagModule } from 'ng-zorro-antd/tag';

import type { HoSoTienDoItem } from '../../api/models/ho-so-tien-do-item';
import type { ThongBaoItem } from '../../api/models/thong-bao-item';
import { TienDoService } from '../../api/services/tien-do.service';
import { TienDoTaiService } from './tien-do-tai.service';
import { ngayVn, nhanLoaiDonTienDo, nhanTrangThaiMoc, tenMoc } from './tien-do-nhan';

/** Tab SV: timeline 3 mốc, nộp báo cáo (file + % + cam kết AI C07), phiếu giao nhiệm vụ, xin gia hạn / dừng, thông báo. */
@Component({
  selector: 'app-tab-sv-tien-do',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    DecimalPipe,
    FormsModule,
    NzAlertModule,
    NzButtonModule,
    NzCheckboxModule,
    NzDrawerModule,
    NzInputModule,
    NzInputNumberModule,
    NzModalModule,
    NzSelectModule,
    NzTagModule,
  ],
  template: `
    @if (hoSo(); as h) {
      @if (h.coDonDangCho) {
        <nz-alert class="mb" nzType="info" nzShowIcon nzMessage="Bạn đang có đơn chờ duyệt." />
      }
      <div class="thanh">
        <button nz-button nzType="primary" (click)="moNop()">Nộp báo cáo tiến độ</button>
        <button nz-button (click)="moDon('GiaHan')">Xin gia hạn mốc</button>
        <button nz-button nzDanger (click)="moDon('XinDung')">Xin dừng ĐATN</button>
        @if (h.coPhieu) {
          <button nz-button (click)="taiPhieu()">Tải phiếu giao nhiệm vụ (PDF)</button>
        }
      </div>

      <div class="timeline">
        @for (m of h.cacMoc; track m.mocTienDoId) {
          <div class="moc" [class.qua-han]="m.trangThai === 'KhongNop'">
            <div class="tieu-de">{{ moc(m.maMoc) }}</div>
            <div class="han">
              Hạn: <strong>{{ ngay(m.hanHieuLuc) }}</strong>
              @if (m.daGiaHan) {
                <nz-tag nzColor="blue">Đã gia hạn</nz-tag>
              }
              @if (m.yeuCauToiThieuPct) {
                <span class="phu">· yêu cầu tối thiểu {{ m.yeuCauToiThieuPct }}%</span>
              }
            </div>
            <div>
              <nz-tag [nzColor]="nhan(m.trangThai).color">{{ nhan(m.trangThai).label }}</nz-tag>
            </div>
            @if (m.baoCaoMoiNhat; as b) {
              <div class="phu">
                Đã nộp {{ b.ngayNop | date: 'dd/MM/yyyy HH:mm' }} ·
                {{ b.tyLeHoanThanhPct | number: '1.0-0' }}%
                @if (b.tenFile) {
                  · {{ b.tenFile }}
                }
                @if (b.diemTienDo !== null && b.diemTienDo !== undefined) {
                  · Điểm tiến độ {{ b.diemTienDo | number: '1.1-1' }}
                }
              </div>
              @if (b.gvhdNhanXet) {
                <div class="nhan-xet"><strong>GVHD nhận xét:</strong> {{ b.gvhdNhanXet }}</div>
              }
            }
          </div>
        }
      </div>
    } @else {
      <nz-alert nzType="info" nzShowIcon nzMessage="Bạn chưa có hồ sơ ĐATN trong đợt này." />
    }

    <h4>Thông báo</h4>
    @for (t of thongBao(); track t.id) {
      <div
        class="tb"
        role="button"
        tabindex="0"
        [class.chua-doc]="!t.daDoc"
        (click)="danhDau(t)"
        (keydown.enter)="danhDau(t)"
      >
        <strong>{{ t.tieuDe }}</strong>
        <div class="phu">{{ t.noiDung }}</div>
      </div>
    } @empty {
      <p class="phu">Không có thông báo.</p>
    }

    <nz-drawer
      [nzVisible]="formNop()"
      [nzWidth]="480"
      nzTitle="Nộp báo cáo tiến độ"
      (nzOnClose)="formNop.set(false)"
    >
      <ng-container *nzDrawerContent>
        <p class="phu">File PDF hoặc DOCX, tối đa 50 MB. Chỉ nộp khi hồ sơ đang thực hiện.</p>
        <div class="dong">
          <span>Mốc</span>
          <nz-select
            [ngModel]="mocChon()"
            (ngModelChange)="mocChon.set($event)"
            aria-label="Chọn mốc"
          >
            @for (m of hoSo()?.cacMoc ?? []; track m.mocTienDoId) {
              <nz-option [nzValue]="m.mocTienDoId" [nzLabel]="moc(m.maMoc)" />
            }
          </nz-select>
        </div>
        <div class="dong">
          <span>Tỉ lệ hoàn thành (%)</span>
          <nz-input-number
            [ngModel]="tyLe()"
            (ngModelChange)="tyLe.set($event ?? 0)"
            [nzMin]="0"
            [nzMax]="100"
            aria-label="Tỉ lệ hoàn thành"
          />
        </div>
        <div class="dong">
          <span>File báo cáo</span>
          <input
            type="file"
            accept=".pdf,.docx"
            (change)="chonFile($event)"
            aria-label="File báo cáo"
          />
        </div>
        <!-- eslint-disable-next-line @angular-eslint/template/label-has-associated-control -- NG-ZORRO dựng <input> bên trong label -->
        <label nz-checkbox [ngModel]="camKet()" (ngModelChange)="camKet.set($event)">
          Tôi cam kết không dùng AI để viết hộ hoặc bịa số liệu trong báo cáo này (C07).
        </label>
        <div class="dong">
          <button
            nz-button
            nzType="primary"
            [disabled]="!file() || !camKet() || !mocChon()"
            [nzLoading]="dangGui()"
            (click)="gui()"
          >
            Nộp báo cáo
          </button>
        </div>
      </ng-container>
    </nz-drawer>

    <nz-modal
      [nzVisible]="donLoai() !== null"
      [nzTitle]="'Gửi đơn: ' + nhanDon(donLoai() ?? '')"
      nzOkText="Gửi đơn"
      [nzOkDisabled]="!donNoiDung().trim()"
      [nzOkLoading]="dangGui()"
      (nzOnCancel)="donLoai.set(null)"
      (nzOnOk)="guiDon()"
    >
      <ng-container *nzModalContent>
        <textarea
          nz-input
          rows="4"
          maxlength="4000"
          [ngModel]="donNoiDung()"
          (ngModelChange)="donNoiDung.set($event)"
          aria-label="Nội dung đơn"
        ></textarea>
        <p class="phu">
          Gia hạn do GVHD duyệt (hạn mới phải sau hạn cũ). Xin dừng do Trưởng Bộ môn duyệt.
        </p>
      </ng-container>
    </nz-modal>
  `,
  styles: `
    .thanh {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin-bottom: 12px;
    }
    .timeline {
      display: grid;
      gap: 12px;
    }
    .moc {
      border: 1px solid #f0f0f0;
      border-radius: 8px;
      padding: 12px;
    }
    .moc.qua-han {
      border-color: #ffa39e;
      background: #fff1f0;
    }
    .tieu-de {
      font-weight: 600;
      margin-bottom: 4px;
    }
    .han {
      margin-bottom: 6px;
    }
    .phu {
      color: rgba(0, 0, 0, 0.55);
      font-size: 13px;
    }
    .nhan-xet {
      margin-top: 6px;
      background: #fafafa;
      padding: 8px;
      border-radius: 6px;
    }
    .mb {
      margin-bottom: 12px;
    }
    h4 {
      margin: 24px 0 8px;
    }
    .tb {
      border: 1px solid #f0f0f0;
      border-radius: 6px;
      padding: 8px 12px;
      margin-bottom: 8px;
      cursor: pointer;
    }
    .tb.chua-doc {
      border-color: #1677ff;
    }
    .dong {
      margin-bottom: 14px;
    }
    .dong label {
      display: block;
      margin-bottom: 4px;
    }
  `,
})
export class TabSvTienDoComponent implements OnInit {
  readonly dotId = input.required<number>();

  private readonly api = inject(TienDoService);
  private readonly taiFile = inject(TienDoTaiService);
  private readonly message = inject(NzMessageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly moc = tenMoc;
  protected readonly nhan = nhanTrangThaiMoc;
  protected readonly nhanDon = nhanLoaiDonTienDo;
  protected readonly ngay = ngayVn;

  protected readonly hoSo = signal<HoSoTienDoItem | null>(null);
  protected readonly thongBao = signal<ThongBaoItem[]>([]);
  protected readonly formNop = signal(false);
  protected readonly mocChon = signal<number | null>(null);
  protected readonly tyLe = signal(0);
  protected readonly camKet = signal(false);
  protected readonly file = signal<File | null>(null);
  protected readonly dangGui = signal(false);
  protected readonly donLoai = signal<string | null>(null);
  protected readonly donNoiDung = signal('');

  ngOnInit(): void {
    this.tai();
    this.taiThongBao();
  }

  protected moNop(): void {
    this.mocChon.set(
      this.hoSo()?.cacMoc.find(
        (m) => m.trangThai === null || m.trangThai === 'CanhBao' || m.trangThai === 'KhongNop',
      )?.mocTienDoId ?? null,
    );
    this.tyLe.set(0);
    this.camKet.set(false);
    this.file.set(null);
    this.formNop.set(true);
  }

  protected chonFile(e: Event): void {
    const input = e.target as HTMLInputElement;
    this.file.set(input.files?.[0] ?? null);
  }

  protected gui(): void {
    const h = this.hoSo();
    const f = this.file();
    const mocId = this.mocChon();
    if (!h || !f || mocId == null || !this.camKet()) return;
    this.dangGui.set(true);
    this.api
      .baoCaoTienDoNop({
        id: h.svTrongDotId,
        body: { MocTienDoId: mocId, TyLeHoanThanhPct: this.tyLe(), CamKetAI: true, File: f },
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.dangGui.set(false);
          this.formNop.set(false);
          this.message.success('Đã nộp báo cáo tiến độ.');
          this.tai();
        },
        error: () => this.dangGui.set(false),
      });
  }

  protected moDon(loai: string): void {
    this.donLoai.set(loai);
    this.donNoiDung.set('');
  }

  protected guiDon(): void {
    const h = this.hoSo();
    const loai = this.donLoai();
    if (!h || !loai) return;
    this.dangGui.set(true);
    this.api
      .donTienDoTao({
        id: h.svTrongDotId,
        body: { loaiDon: loai as never, noiDung: this.donNoiDung().trim() },
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.dangGui.set(false);
          this.donLoai.set(null);
          this.message.success('Đã gửi đơn.');
          this.tai();
        },
        error: () => this.dangGui.set(false),
      });
  }

  protected taiPhieu(): void {
    const h = this.hoSo();
    if (!h) return;
    this.taiFile
      .taiPhieuPdf(h.svTrongDotId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `phieu-giao-nhiem-vu-${h.maSV}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
      });
  }

  protected danhDau(t: ThongBaoItem): void {
    if (t.daDoc) return;
    this.api
      .thongBaoDaDoc({ id: t.id })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.taiThongBao());
  }

  private tai(): void {
    this.api
      .tienDoCuaToi({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) => this.hoSo.set(ds[0] ?? null));
  }

  private taiThongBao(): void {
    this.api
      .thongBaoDanhSach()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) => this.thongBao.set(ds));
  }
}
