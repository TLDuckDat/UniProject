import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, linkedSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzDescriptionsModule } from 'ng-zorro-antd/descriptions';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';

import type { XetGiaoSnapshotItem } from '../../../api/models/xet-giao-snapshot-item';
import { moTaNgoaiLe, nhanCo, nhanKetQua, nhanLoaiNo, nhanLoaiXet } from './xet-giao-nhan';

/**
 * Snapshot xét giao ĐATN dạng bảng cho người đọc không phải dev: kết luận, từng điều kiện G1–G3,
 * cờ, và từng học phần chưa tích lũy (có tính vào giới hạn hay không, vì sao, mã ngoại lệ).
 * Nhận toàn bộ lịch sử snapshot; mặc định hiển thị lần xét mới nhất, chọn được lần cũ (A19).
 */
@Component({
  selector: 'app-snapshot-xet-giao',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    FormsModule,
    NgTemplateOutlet,
    NzAlertModule,
    NzDescriptionsModule,
    NzEmptyModule,
    NzSelectModule,
    NzTableModule,
    NzTagModule,
    NzTooltipModule,
  ],
  template: `
    @if (dangXem(); as s) {
      @if (snapshots().length > 1) {
        <div class="lan-xet">
          <span>Lần xét:</span>
          <nz-select
            [ngModel]="dangXemId()"
            (ngModelChange)="dangXemId.set($event)"
            aria-label="Chọn lần xét"
          >
            @for (x of snapshots(); track x.id; let i = $index) {
              <nz-option
                [nzValue]="x.id"
                [nzLabel]="
                  'Lần ' +
                  (i + 1) +
                  ' – ' +
                  loaiXet(x.loaiXet) +
                  ' – ' +
                  (x.thoiDiemXet | date: 'dd/MM/yyyy HH:mm') +
                  (x.id === moiNhat()?.id ? ' (hiệu lực)' : '')
                "
              />
            }
          </nz-select>
        </div>
      }

      <nz-descriptions
        nzBordered
        nzSize="small"
        [nzColumn]="{ xxl: 3, xl: 3, lg: 2, md: 2, sm: 1, xs: 1 }"
      >
        <nz-descriptions-item nzTitle="Kết luận">
          <nz-tag [nzColor]="ketQua(s.ketQua).color">{{ ketQua(s.ketQua).label }}</nz-tag>
          @if (s.maNgoaiLe) {
            <nz-tag nz-tooltip [nzTooltipTitle]="moTa(s.maNgoaiLe)">{{ s.maNgoaiLe }}</nz-tag>
          }
        </nz-descriptions-item>
        <nz-descriptions-item nzTitle="TC chưa tích lũy (tính vào giới hạn)">
          <strong [class.vuot]="s.tcChuaTichLuy > s.maxTCChuaTichLuy"
            >{{ s.tcChuaTichLuy }} / tối đa {{ s.maxTCChuaTichLuy }} TC</strong
          >
        </nz-descriptions-item>
        <nz-descriptions-item nzTitle="Quy chế áp dụng">{{ s.maRuleSet }}</nz-descriptions-item>
        <nz-descriptions-item nzTitle="Kỷ luật / hình sự (G1)">
          <ng-container *ngTemplateOutlet="dieuKien; context: { $implicit: s.g1KyLuat }" />
        </nz-descriptions-item>
        <nz-descriptions-item nzTitle="Trạng thái học tập, thời hạn (G2)">
          <ng-container *ngTemplateOutlet="dieuKien; context: { $implicit: s.g2TrangThaiHocTap }" />
        </nz-descriptions-item>
        <nz-descriptions-item nzTitle="Tín chỉ chưa tích lũy (G3)">
          <ng-container *ngTemplateOutlet="dieuKien; context: { $implicit: s.g3TinChi }" />
        </nz-descriptions-item>
        <nz-descriptions-item nzTitle="Thực tập tốt nghiệp">
          {{ s.daDatTTTN ? 'Đã đạt' : 'Chưa đạt' }}
        </nz-descriptions-item>
        <nz-descriptions-item nzTitle="Thời điểm xét">
          {{ s.thoiDiemXet | date: 'dd/MM/yyyy HH:mm' }} ({{ loaiXet(s.loaiXet) }})
        </nz-descriptions-item>
        <nz-descriptions-item nzTitle="Người chạy">{{
          s.nguoiChay ?? 'Hệ thống'
        }}</nz-descriptions-item>
      </nz-descriptions>

      @for (c of s.co; track c) {
        <nz-alert
          class="co"
          nzType="warning"
          nzShowIcon
          [nzMessage]="co(c).label"
          [nzDescription]="co(c).moTa"
        />
      }
      @if (s.lyDo) {
        <p class="ly-do"><strong>Giải thích:</strong> {{ s.lyDo }}</p>
      }

      <h4>Học phần chưa tích lũy</h4>
      <nz-table
        #bang
        nzSize="small"
        [nzData]="s.chiTiets"
        [nzShowPagination]="false"
        [nzNoResult]="khongNo"
        nzTableLayout="fixed"
      >
        <thead>
          <tr>
            <th nzWidth="100px">Mã HP</th>
            <th>Tên học phần</th>
            <th nzWidth="60px" nzAlign="right">TC</th>
            <th nzWidth="170px">Tình trạng</th>
            <th nzWidth="120px">Tính vào giới hạn</th>
            <th nzWidth="80px">Mã</th>
            <th>Giải thích</th>
          </tr>
        </thead>
        <tbody>
          @for (c of bang.data; track c.maHP) {
            <tr [class.loai-tru]="c.biLoaiTru">
              <td>{{ c.maHP }}</td>
              <td>{{ c.tenHP }}</td>
              <td class="so">{{ c.soTC }}</td>
              <td>{{ loaiNo(c.loaiNo) }}</td>
              <td>
                @if (c.biLoaiTru) {
                  <nz-tag>Không (loại trừ)</nz-tag>
                } @else {
                  <nz-tag nzColor="red">Có</nz-tag>
                }
              </td>
              <td>
                @if (c.maNgoaiLe) {
                  <span nz-tooltip [nzTooltipTitle]="moTa(c.maNgoaiLe)">{{ c.maNgoaiLe }}</span>
                }
              </td>
              <td>{{ c.lyDo }}</td>
            </tr>
          }
        </tbody>
        @if (s.chiTiets.length > 0) {
          <tfoot>
            <tr>
              <td colspan="2"><strong>Tổng tính vào giới hạn</strong></td>
              <td class="so">
                <strong>{{ tongTinh() }}</strong>
              </td>
              <td colspan="4"></td>
            </tr>
          </tfoot>
        }
      </nz-table>
      <ng-template #khongNo
        ><nz-empty nzNotFoundContent="Không còn học phần nào chưa tích lũy."
      /></ng-template>
    } @else {
      <nz-empty nzNotFoundContent="Chưa có kết quả xét giao." />
    }

    <ng-template #dieuKien let-dat>
      @if (dat === true) {
        <nz-tag nzColor="green">Đạt</nz-tag>
      } @else if (dat === false) {
        <nz-tag nzColor="red">Không đạt</nz-tag>
      } @else {
        <span>—</span>
      }
    </ng-template>
  `,
  styles: `
    .lan-xet {
      display: flex;
      gap: 8px;
      align-items: center;
      margin-bottom: 12px;
    }
    .lan-xet nz-select {
      min-width: 320px;
      max-width: 100%;
    }
    .co {
      margin-top: 12px;
    }
    .ly-do {
      margin: 12px 0;
    }
    h4 {
      margin: 16px 0 8px;
    }
    .so {
      text-align: right;
    }
    .vuot {
      color: #cf1322;
    }
    .loai-tru {
      color: rgba(0, 0, 0, 0.45);
    }
  `,
})
export class SnapshotXetGiaoComponent {
  /** Toàn bộ snapshot của hồ sơ, theo thứ tự thời gian (server đã sắp). */
  readonly snapshots = input.required<readonly XetGiaoSnapshotItem[]>();

  protected readonly moiNhat = computed(() => this.snapshots().at(-1) ?? null);
  protected readonly dangXemId = linkedSignal(() => this.moiNhat()?.id ?? null);
  protected readonly dangXem = computed(
    () => this.snapshots().find((x) => x.id === this.dangXemId()) ?? this.moiNhat(),
  );
  protected readonly tongTinh = computed(() =>
    (this.dangXem()?.chiTiets ?? []).filter((c) => !c.biLoaiTru).reduce((t, c) => t + c.soTC, 0),
  );

  protected readonly ketQua = nhanKetQua;
  protected readonly loaiNo = nhanLoaiNo;
  protected readonly co = nhanCo;
  protected readonly moTa = moTaNgoaiLe;
  protected readonly loaiXet = nhanLoaiXet;
}
