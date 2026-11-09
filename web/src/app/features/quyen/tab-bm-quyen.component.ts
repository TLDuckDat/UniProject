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
import { FormsModule } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';

import type { GiangVienTomTat } from '../../api/models/giang-vien-tom-tat';
import type { GvddTreHanItem } from '../../api/models/gvdd-tre-han-item';
import { DeTaiService } from '../../api/services/de-tai.service';
import { QuyenDocDuyetService } from '../../api/services/quyen-doc-duyet.service';

/** Tab Bộ môn: GVĐD quá hạn đọc quyển (D09) → thay GVĐD (SV không bị tính lỗi). */
@Component({
  selector: 'app-tab-bm-quyen',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    FormsModule,
    NzAlertModule,
    NzButtonModule,
    NzEmptyModule,
    NzInputModule,
    NzModalModule,
    NzSelectModule,
    NzTableModule,
    NzTagModule,
  ],
  template: `
    <nz-alert
      class="mb"
      nzType="info"
      nzShowIcon
      nzMessage="GVĐD quá hạn đọc quyển: Trưởng Bộ môn thay GVĐD. Sinh viên không bị tính lỗi (D09)."
    />

    <nz-table
      nzSize="middle"
      [nzData]="treHan()"
      [nzShowPagination]="false"
      [nzLoading]="dangTai()"
    >
      <thead>
        <tr>
          <th>Sinh viên</th>
          <th>GVĐD hiện tại</th>
          <th>Nộp quyển</th>
          <th>Quá hạn</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        @for (t of treHan(); track t.svTrongDotId) {
          <tr>
            <td>{{ t.maSV }} – {{ t.hoTen }}</td>
            <td>{{ t.maGVDD }} – {{ t.tenGVDD }}</td>
            <td>{{ t.ngayNopBan | date: 'dd/MM/yyyy' }}</td>
            <td>
              <nz-tag nzColor="red">{{ t.soNgayQuaHan }} ngày</nz-tag>
            </td>
            <td><button nz-button nzType="link" (click)="moThay(t)">Thay GVĐD</button></td>
          </tr>
        } @empty {
          <tr>
            <td colspan="5"><nz-empty nzNotFoundContent="Không có GVĐD trễ hạn đọc quyển." /></td>
          </tr>
        }
      </tbody>
    </nz-table>

    <nz-modal
      [nzVisible]="dangThay() !== null"
      [nzTitle]="'Thay GVĐD – ' + (dangThay()?.maSV ?? '')"
      nzOkText="Thay GVĐD"
      [nzOkDisabled]="!giangVienMoi || !lyDo.trim()"
      [nzOkLoading]="dangLuu()"
      (nzOnCancel)="dangThay.set(null)"
      (nzOnOk)="thay()"
    >
      <ng-container *nzModalContent>
        <span>GVĐD mới</span>
        <nz-select
          [(ngModel)]="giangVienMoi"
          nzShowSearch
          nzPlaceHolder="Chọn giảng viên"
          class="full"
          aria-label="GVĐD mới"
        >
          @for (g of gvs(); track g.id) {
            <nz-option [nzValue]="g.id" [nzLabel]="g.maGV + ' – ' + g.hoTen" />
          }
        </nz-select>
        <span class="mt">Lý do</span>
        <textarea
          nz-input
          rows="3"
          maxlength="300"
          [(ngModel)]="lyDo"
          aria-label="Lý do thay GVĐD"
        ></textarea>
      </ng-container>
    </nz-modal>
  `,
  styles: `
    .mb {
      display: block;
      margin-bottom: 12px;
    }
    .full {
      width: 100%;
      margin-bottom: 12px;
    }
    .mt {
      display: block;
      margin: 12px 0 4px;
    }
  `,
})
export class TabBmQuyenComponent implements OnInit {
  readonly dotId = input.required<number>();

  private readonly api = inject(QuyenDocDuyetService);
  private readonly deTai = inject(DeTaiService);
  private readonly message = inject(NzMessageService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly treHan = signal<GvddTreHanItem[]>([]);
  protected readonly gvs = signal<GiangVienTomTat[]>([]);
  protected readonly dangTai = signal(false);
  protected readonly dangLuu = signal(false);
  protected readonly dangThay = signal<GvddTreHanItem | null>(null);
  protected giangVienMoi: number | null = null;
  protected lyDo = '';

  ngOnInit(): void {
    this.tai();
    this.deTai
      .deTaiGvDeNghi({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ds) => this.gvs.set(ds));
  }

  protected moThay(t: GvddTreHanItem): void {
    this.giangVienMoi = null;
    this.lyDo = '';
    this.dangThay.set(t);
  }

  protected thay(): void {
    const t = this.dangThay();
    if (!t || !this.giangVienMoi || !this.lyDo.trim()) return;
    this.dangLuu.set(true);
    this.api
      .quyenThayGvdd({
        id: t.svTrongDotId,
        body: { giangVienMoiId: this.giangVienMoi, lyDo: this.lyDo.trim() },
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.dangLuu.set(false);
          this.dangThay.set(null);
          this.message.success('Đã thay GVĐD.');
          this.tai();
        },
        error: () => this.dangLuu.set(false),
      });
  }

  private tai(): void {
    this.dangTai.set(true);
    this.api
      .quyenGvddTreHan({ dotId: this.dotId() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (ds) => {
          this.treHan.set(ds);
          this.dangTai.set(false);
        },
        error: () => this.dangTai.set(false),
      });
  }
}
