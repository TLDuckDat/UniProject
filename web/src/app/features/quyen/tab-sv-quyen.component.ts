import { DatePipe } from '@angular/common';
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
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';

import type { PhienBanQuyenItem } from '../../api/models/phien-ban-quyen-item';
import { HoSoCuaToiService } from '../../api/services/ho-so-cua-toi.service';
import { QuyenDocDuyetService } from '../../api/services/quyen-doc-duyet.service';
import { hashRutGon, nhanLoaiBan } from './quyen-nhan';

/** Tab SV: nộp quyển (kéo-thả hoặc chọn file), lịch sử phiên bản kèm hash SHA-256. */
@Component({
  selector: 'app-tab-sv-quyen',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, NzAlertModule, NzButtonModule, NzTableModule, NzTagModule],
  template: `
    @if (!svTrongDotId()) {
      <nz-alert nzType="info" nzShowIcon nzMessage="Bạn chưa có hồ sơ ĐATN trong đợt này." />
    } @else {
      <nz-alert class="mb" nzType="info" nzShowIcon [nzMessage]="huongDan()" />

      <div
        class="vung-tha"
        [class.dang-keo]="dangKeo()"
        role="button"
        tabindex="0"
        (dragover)="keo($event)"
        (dragleave)="dangKeo.set(false)"
        (drop)="tha($event)"
        (click)="chonFile.click()"
        (keydown.enter)="chonFile.click()"
      >
        <div>Kéo file PDF / DOCX vào đây, hoặc bấm để chọn</div>
        <div class="phu">Tối đa 50 MB</div>
        <input #chonFile type="file" accept=".pdf,.docx" hidden (change)="chon($event)" />
      </div>
      @if (fileDaChon(); as f) {
        <div class="chon">
          Đã chọn: <strong>{{ f.name }}</strong>
          <button nz-button nzType="primary" [nzLoading]="dangNop()" (click)="nop()">Nộp</button>
        </div>
      }

      <h4>Lịch sử phiên bản</h4>
      <nz-table nzSize="small" [nzData]="phienBan()" [nzShowPagination]="false">
        <thead>
          <tr>
            <th>Phiên bản</th>
            <th>Loại</th>
            <th>Tên file</th>
            <th>Ngày nộp</th>
            <th>SHA-256</th>
            <th>Ghi chú</th>
          </tr>
        </thead>
        <tbody>
          @for (p of phienBan(); track p.id) {
            <tr>
              <td>v{{ p.soPhienBan }}</td>
              <td>{{ loai(p.loaiBan) }}</td>
              <td>{{ p.tenFile }}</td>
              <td>{{ p.ngayNop | date: 'dd/MM/yyyy HH:mm' }}</td>
              <td>
                <code [title]="p.hashSHA256">{{ rutGon(p.hashSHA256) }}</code>
              </td>
              <td>
                @if (p.nopQuaHan) {
                  <nz-tag nzColor="orange">Nộp sau hạn (có gia hạn)</nz-tag>
                }
                @if (p.laBanBaoVe) {
                  <nz-tag nzColor="green">Bản bảo vệ</nz-tag>
                }
              </td>
            </tr>
          } @empty {
            <tr>
              <td colspan="6">Chưa nộp quyển.</td>
            </tr>
          }
        </tbody>
      </nz-table>
    }
  `,
  styles: `
    .mb {
      display: block;
      margin-bottom: 12px;
    }
    .vung-tha {
      border: 2px dashed #d9d9d9;
      border-radius: 8px;
      padding: 24px;
      text-align: center;
      cursor: pointer;
      margin-bottom: 12px;
    }
    .vung-tha.dang-keo {
      border-color: #1677ff;
      background: #e6f4ff;
    }
    .phu {
      color: rgba(0, 0, 0, 0.55);
      font-size: 12px;
    }
    .chon {
      display: flex;
      gap: 12px;
      align-items: center;
      margin-bottom: 16px;
    }
    h4 {
      margin: 16px 0 8px;
    }
  `,
})
export class TabSvQuyenComponent implements OnInit {
  readonly dotId = input.required<number>();

  private readonly hoSoApi = inject(HoSoCuaToiService);
  private readonly api = inject(QuyenDocDuyetService);
  private readonly message = inject(NzMessageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loai = nhanLoaiBan;
  protected readonly rutGon = hashRutGon;

  protected readonly svTrongDotId = signal<number | null>(null);
  protected readonly phienBan = signal<PhienBanQuyenItem[]>([]);
  protected readonly fileDaChon = signal<File | null>(null);
  protected readonly dangKeo = signal(false);
  protected readonly dangNop = signal(false);
  protected readonly huongDan = signal(
    'Nộp quyển (PDF hoặc DOCX). Nộp đúng hạn để tránh quá hạn; bản sửa gấp chỉ nộp khi GVĐD yêu cầu.',
  );

  ngOnInit(): void {
    this.hoSoApi
      .hoSoCuaToiXetGiao()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) => {
        const id = ds.find((x) => x.dotId === this.dotId())?.svTrongDotId ?? null;
        this.svTrongDotId.set(id);
        if (id) this.taiLichSu(id);
      });
  }

  protected keo(e: DragEvent): void {
    e.preventDefault();
    this.dangKeo.set(true);
  }

  protected tha(e: DragEvent): void {
    e.preventDefault();
    this.dangKeo.set(false);
    this.chonTep(e.dataTransfer?.files?.[0] ?? null);
  }

  protected chon(e: Event): void {
    this.chonTep((e.target as HTMLInputElement).files?.[0] ?? null);
  }

  private chonTep(f: File | null): void {
    if (!f) return;
    if (!/\.(pdf|docx)$/i.test(f.name)) {
      this.message.warning('Chỉ nhận file PDF hoặc DOCX.');
      return;
    }
    this.fileDaChon.set(f);
  }

  protected nop(): void {
    const id = this.svTrongDotId();
    const f = this.fileDaChon();
    if (!id || !f) return;
    this.dangNop.set(true);
    this.api
      .quyenNop({ id, body: { File: f } })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (pb) => {
          this.dangNop.set(false);
          this.fileDaChon.set(null);
          this.message.success(
            `Đã nộp ${nhanLoaiBan(pb.loaiBan).toLowerCase()} v${pb.soPhienBan}.`,
          );
          this.taiLichSu(id);
        },
        error: () => this.dangNop.set(false),
      });
  }

  private taiLichSu(id: number): void {
    this.api
      .quyenLichSu({ id })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) => this.phienBan.set(ds));
  }
}
