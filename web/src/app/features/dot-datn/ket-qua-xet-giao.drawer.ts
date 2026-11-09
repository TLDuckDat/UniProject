import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { forkJoin } from 'rxjs';

import type { HoSoXetGiaoItem } from '../../api/models/ho-so-xet-giao-item';
import type { XetGiaoSnapshotItem } from '../../api/models/xet-giao-snapshot-item';
import { WorkflowService } from '../../api/services/workflow.service';
import { XetGiaoService } from '../../api/services/xet-giao.service';
import { AuthService } from '../../core/auth/auth.service';
import { Quyen } from '../../core/auth/quyen';
import {
  LichSuTrangThaiItem,
  LichSuTrangThaiTimelineComponent,
} from '../../shared/components/lich-su-trang-thai-timeline/lich-su-trang-thai-timeline.component';
import { SnapshotXetGiaoComponent } from '../../shared/components/snapshot-xet-giao/snapshot-xet-giao.component';

const CO_THE_XET_LAI = new Set(['CHO_XET', 'KHONG_DU_DK', 'KHONG_DU_DK_CHUYEN_THAY_THE']);

/** Drawer "Kết quả xét giao" của một hồ sơ: snapshot dạng bảng, lịch sử trạng thái, nút xét lại (A19). */
@Component({
  selector: 'app-ket-qua-xet-giao-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NzButtonModule,
    NzDrawerModule,
    NzPopconfirmModule,
    NzSpinModule,
    LichSuTrangThaiTimelineComponent,
    SnapshotXetGiaoComponent,
  ],
  template: `
    <nz-drawer
      [nzVisible]="hoSo() !== null"
      [nzWidth]="'min(960px, 100vw)'"
      [nzTitle]="hoSo() ? 'Kết quả xét giao – ' + hoSo()!.maSV + ' ' + hoSo()!.hoTen : ''"
      [nzExtra]="hanhDong"
      (nzOnClose)="dong.emit()"
    >
      <ng-container *nzDrawerContent>
        @if (hoSo(); as h) {
          <p class="tom-tat">
            CTĐT {{ h.maCTDT }} – {{ h.tenCTDT }} · Khóa K{{ h.khoaHoc }}
            @if (h.lanLamLai > 0) {
              · Làm lại lần {{ h.lanLamLai }} (A21)
            }
            @if (h.soQuyetDinhGiao) {
              · QĐ giao số {{ h.soQuyetDinhGiao }}
            }
          </p>
        }
        <nz-spin [nzSpinning]="dangTai()">
          <app-snapshot-xet-giao [snapshots]="snapshots()" />
          <h4>Lịch sử trạng thái</h4>
          <app-lich-su-trang-thai-timeline [lichSu]="lichSu()" />
        </nz-spin>
      </ng-container>
    </nz-drawer>
    <ng-template #hanhDong>
      @if (coTheXetLai()) {
        <button
          nz-button
          nz-popconfirm
          nzPopconfirmTitle="Xét lại với dữ liệu điểm hiện tại? Kết quả cũ vẫn được lưu."
          (nzOnConfirm)="xetLai()"
          [nzLoading]="dangXetLai()"
        >
          Xét lại
        </button>
      }
    </ng-template>
  `,
  styles: `
    .tom-tat {
      color: rgba(0, 0, 0, 0.65);
    }
    h4 {
      margin: 24px 0 8px;
    }
  `,
})
export class KetQuaXetGiaoDrawer {
  private readonly xetGiaoApi = inject(XetGiaoService);
  private readonly workflowApi = inject(WorkflowService);
  private readonly message = inject(NzMessageService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  /** Hồ sơ đang xem; null = đóng drawer. */
  readonly hoSo = input<HoSoXetGiaoItem | null>(null);
  readonly dong = output<void>();
  /** Phát ra sau khi xét lại thành công để danh sách tải lại. */
  readonly daXetLai = output<XetGiaoSnapshotItem>();

  protected readonly snapshots = signal<XetGiaoSnapshotItem[]>([]);
  protected readonly lichSu = signal<LichSuTrangThaiItem[]>([]);
  protected readonly dangTai = signal(false);
  protected readonly dangXetLai = signal(false);
  protected readonly coTheXetLai = computed(() => {
    const h = this.hoSo();
    return (
      !!h &&
      !h.soQuyetDinhGiao &&
      CO_THE_XET_LAI.has(h.trangThai) &&
      this.auth.coQuyen(Quyen.XetDieuKienChay)
    );
  });

  constructor() {
    effect(() => {
      const h = this.hoSo();
      if (h) {
        this.tai(h.id);
      }
    });
  }

  protected xetLai(): void {
    const h = this.hoSo();
    if (!h) {
      return;
    }
    this.dangXetLai.set(true);
    this.xetGiaoApi
      .xetGiaoXetLai({ id: h.id })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (s) => {
          this.dangXetLai.set(false);
          this.message.success('Đã xét lại; kết quả cũ vẫn được lưu trong lịch sử.');
          this.daXetLai.emit(s);
          this.tai(h.id);
        },
        error: () => this.dangXetLai.set(false),
      });
  }

  private tai(id: number): void {
    this.dangTai.set(true);
    forkJoin({
      snapshots: this.xetGiaoApi.xetGiaoSnapshot({ id }),
      lichSu: this.workflowApi.apiV1SvtrongdotIdLichSuTrangThaiGet({ id }),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ snapshots, lichSu }) => {
          this.snapshots.set(snapshots);
          this.lichSu.set(
            lichSu.map((x) => ({
              tuTrangThai: x.tuTrangThai ?? null,
              denTrangThai: x.denTrangThai,
              lyDo: x.lyDo ?? null,
              nguoiThucHienId: x.nguoiThucHienId ?? null,
              thoiDiem: x.thoiDiem,
            })),
          );
          this.dangTai.set(false);
        },
        error: () => this.dangTai.set(false),
      });
  }
}
