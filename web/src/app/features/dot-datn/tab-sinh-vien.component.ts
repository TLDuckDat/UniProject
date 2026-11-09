import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  OnInit,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzProgressModule } from 'ng-zorro-antd/progress';
import { NzRadioModule } from 'ng-zorro-antd/radio';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTableModule, NzTableQueryParams } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzTooltipModule } from 'ng-zorro-antd/tooltip';
import { Subscription, switchMap, takeWhile, timer } from 'rxjs';

import type { DotDatnItem } from '../../api/models/dot-datn-item';
import type { HoSoXetGiaoItem } from '../../api/models/ho-so-xet-giao-item';
import type { SinhVienUngVienItem } from '../../api/models/sinh-vien-ung-vien-item';
import type { ThongKeTrangThaiItem } from '../../api/models/thong-ke-trang-thai-item';
import type { TrangThaiSvTrongDot } from '../../api/models/trang-thai-sv-trong-dot';
import type { XetGiaoJobItem } from '../../api/models/xet-giao-job-item';
import { DotDatnService } from '../../api/services/dot-datn.service';
import { AuthService } from '../../core/auth/auth.service';
import { Quyen } from '../../core/auth/quyen';
import {
  KET_QUA_LOC,
  jobDangChay,
  moTaNgoaiLe,
  nhanCo,
  nhanKetQua,
  nhanTrangThaiJob,
} from '../../shared/components/snapshot-xet-giao/xet-giao-nhan';
import { trangThaiStyle } from '../../shared/components/trang-thai-tag/trang-thai-style';
import { TrangThaiTagComponent } from '../../shared/components/trang-thai-tag/trang-thai-tag.component';
import { KetQuaXetGiaoDrawer } from './ket-qua-xet-giao.drawer';

/** Tab "Sinh viên" của đợt: lọc theo trạng thái/cờ/kết quả, chạy xét giao + tiến độ, thêm SV, xem snapshot. */
@Component({
  selector: 'app-tab-sinh-vien',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    NzAlertModule,
    NzBadgeModule,
    NzButtonModule,
    NzCheckboxModule,
    NzDrawerModule,
    NzIconModule,
    NzInputModule,
    NzInputNumberModule,
    NzPopconfirmModule,
    NzProgressModule,
    NzRadioModule,
    NzSelectModule,
    NzTableModule,
    NzTagModule,
    NzTooltipModule,
    KetQuaXetGiaoDrawer,
    TrangThaiTagComponent,
  ],
  template: `
    @if (dot().trangThai !== 'Dong') {
      <div class="thanh-cong-cu">
        @if (coQuyenDot()) {
          <button nz-button (click)="moThem()"><nz-icon nzType="plus" /> Thêm sinh viên</button>
        }
        @if (coQuyenXet()) {
          <button
            nz-button
            nzType="primary"
            nz-popconfirm
            nzPopconfirmTitle="Xét điều kiện giao cho mọi hồ sơ chờ xét chưa có kết quả? Hồ sơ đã xét không bị xét lại."
            [disabled]="dangChay()"
            (nzOnConfirm)="chayXet()"
          >
            <nz-icon nzType="play-circle" /> Chạy xét điều kiện giao
          </button>
        }
      </div>
    }

    @if (job(); as j) {
      <div class="tien-do" aria-live="polite">
        <span>
          Lượt xét gần nhất: <strong>{{ trangThaiJob(j.trangThai) }}</strong> – {{ j.daXuLy }}/{{
            j.tongSo
          }}
          hồ sơ
          @if (j.soLoi > 0) {
            · <span class="loi">{{ j.soLoi }} lỗi</span>
          }
        </span>
        <nz-progress
          [nzPercent]="phanTram()"
          [nzStatus]="
            j.trangThai === 'Failed' || j.soLoi > 0
              ? 'exception'
              : dangChay()
                ? 'active'
                : 'success'
          "
        />
        @if (j.loi.length > 0) {
          <nz-alert
            nzType="warning"
            nzShowIcon
            nzMessage="Hồ sơ chưa xét được – bổ sung dữ liệu rồi chạy lại (chỉ xét hồ sơ còn thiếu)"
            [nzDescription]="dsLoi"
          />
          <ng-template #dsLoi>
            <ul>
              @for (e of j.loi; track e.svTrongDotId) {
                <li>{{ e.maSV }}: {{ e.message }}</li>
              }
            </ul>
          </ng-template>
        }
      </div>
    }

    <div class="loc">
      <nz-radio-group
        nzButtonStyle="solid"
        [ngModel]="trangThai()"
        (ngModelChange)="trangThai.set($event); taiLai()"
        aria-label="Lọc theo trạng thái"
      >
        <!-- eslint-disable-next-line @angular-eslint/template/label-has-associated-control -- NG-ZORRO dựng <input> bên trong label -->
        <label nz-radio-button [nzValue]="null">Tất cả ({{ tongSo() }})</label>
        @for (t of thongKe(); track t.trangThai) {
          <!-- eslint-disable-next-line @angular-eslint/template/label-has-associated-control -- NG-ZORRO dựng <input> bên trong label -->
          <label nz-radio-button [nzValue]="t.trangThai"
            >{{ nhanTrangThai(t.trangThai) }} ({{ t.soLuong }})</label
          >
        }
      </nz-radio-group>
      <div class="loc-hang">
        <nz-select
          class="ket-qua"
          nzAllowClear
          nzPlaceHolder="Kết quả xét"
          [ngModel]="ketQuaXet()"
          (ngModelChange)="ketQuaXet.set($event); taiLai()"
        >
          @for (k of ketQuaLoc; track k.value) {
            <nz-option [nzValue]="k.value" [nzLabel]="k.label" />
          }
        </nz-select>
        <!-- eslint-disable-next-line @angular-eslint/template/label-has-associated-control -- NG-ZORRO dựng <input> bên trong label -->
        <label
          nz-checkbox
          [ngModel]="coNoTinChi()"
          (ngModelChange)="coNoTinChi.set($event); taiLai()"
          >Còn nợ tín chỉ</label
        >
        <!-- eslint-disable-next-line @angular-eslint/template/label-has-associated-control -- NG-ZORRO dựng <input> bên trong label -->
        <label
          nz-checkbox
          [ngModel]="chuaQuaTTTN()"
          (ngModelChange)="chuaQuaTTTN.set($event); taiLai()"
          >Chưa qua TTTN</label
        >
        <input
          nz-input
          class="tim"
          placeholder="Mã SV, họ tên, CTĐT"
          aria-label="Tìm sinh viên"
          [value]="q()"
          (input)="tim($any($event.target).value)"
        />
      </div>
    </div>

    <nz-table
      #bang
      nzShowSizeChanger
      nzSize="middle"
      [nzData]="items()"
      [nzFrontPagination]="false"
      [nzTotal]="total()"
      [nzPageIndex]="page()"
      [nzPageSize]="pageSize()"
      [nzLoading]="dangTai()"
      (nzQueryParams)="doiTrang($event)"
    >
      <thead>
        <tr>
          <th>Mã SV</th>
          <th>Họ tên</th>
          <th>Khóa</th>
          <th>CTĐT</th>
          <th>Trạng thái</th>
          <th>Kết quả xét</th>
          <th nzAlign="right">TC chưa tích lũy</th>
          <th>Cờ</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        @for (h of bang.data; track h.id) {
          <tr>
            <td>{{ h.maSV }}</td>
            <td>
              {{ h.hoTen }}
              @if (h.lanLamLai > 0) {
                <nz-tag nz-tooltip nzTooltipTitle="Hồ sơ làm lại (A21) – tính là học lại"
                  >Làm lại {{ h.lanLamLai }}</nz-tag
                >
              }
            </td>
            <td>K{{ h.khoaHoc }}</td>
            <td nz-tooltip [nzTooltipTitle]="h.tenCTDT">{{ h.maCTDT }}</td>
            <td><app-trang-thai-tag [trangThai]="h.trangThai" /></td>
            <td>
              <nz-tag [nzColor]="ketQua(h.ketQuaXet).color">{{ ketQua(h.ketQuaXet).label }}</nz-tag>
              @if (h.maNgoaiLe) {
                <span class="ma" nz-tooltip [nzTooltipTitle]="moTa(h.maNgoaiLe)">{{
                  h.maNgoaiLe
                }}</span>
              }
            </td>
            <td class="so">{{ h.tcChuaTichLuy ?? '—' }}</td>
            <td>
              @if (h.coNoTinChi) {
                <nz-tag nzColor="orange" nz-tooltip [nzTooltipTitle]="co('NO_TIN_CHI').moTa"
                  >Nợ TC</nz-tag
                >
              }
              @if (h.chuaQuaTTTN && h.ketQuaXet) {
                <nz-tag nzColor="gold" nz-tooltip [nzTooltipTitle]="co('CHUA_QUA_TTTN').moTa"
                  >Chưa qua TTTN</nz-tag
                >
              }
              @if (h.coKhieuNaiChoXuLy) {
                <nz-badge nzStatus="processing" nzText="Khiếu nại" />
              }
            </td>
            <td>
              <button nz-button nzType="link" type="button" (click)="dangXem.set(h)">
                Xem kết quả
              </button>
            </td>
          </tr>
        }
      </tbody>
    </nz-table>

    <app-ket-qua-xet-giao-drawer
      [hoSo]="dangXem()"
      (dong)="dangXem.set(null)"
      (daXetLai)="sauXetLai()"
    />

    <nz-drawer
      [nzVisible]="themMo()"
      [nzWidth]="'min(860px, 100vw)'"
      nzTitle="Thêm sinh viên vào đợt"
      (nzOnClose)="themMo.set(false)"
    >
      <ng-container *nzDrawerContent>
        <div class="loc-hang">
          <nz-input-number
            [ngModel]="locKhoa()"
            (ngModelChange)="locKhoa.set($event)"
            [nzMin]="1"
            [nzMax]="99"
            nzPlaceHolder="Khóa (vd 65)"
            aria-label="Khóa"
          />
          <input
            nz-input
            class="tim"
            placeholder="Ngành"
            aria-label="Ngành"
            [ngModel]="locNganh()"
            (ngModelChange)="locNganh.set($event)"
          />
          <input
            nz-input
            class="tim"
            placeholder="Mã SV, họ tên"
            aria-label="Từ khóa"
            [ngModel]="locQ()"
            (ngModelChange)="locQ.set($event)"
          />
          <button nz-button (click)="timUngVien()" [nzLoading]="dangTimUngVien()">Tìm</button>
        </div>
        <p class="goi-y">
          SV đang nghỉ học tạm thời / bảo lưu không được đưa vào đợt (A11). SV học 2 CTĐT có 2 hồ sơ
          độc lập (A18). SV có hồ sơ không đạt ở đợt trước được tự động liên kết hồ sơ làm lại
          (A21).
        </p>
        <nz-table
          #ungVienBang
          nzSize="small"
          [nzData]="ungVien()"
          [nzPageSize]="50"
          [nzLoading]="dangTimUngVien()"
        >
          <thead>
            <tr>
              <th
                [nzChecked]="chonTatCa()"
                (nzCheckedChange)="chonHet($event)"
                aria-label="Chọn tất cả"
              ></th>
              <th>Mã SV</th>
              <th>Họ tên</th>
              <th>Khóa</th>
              <th>CTĐT</th>
              <th>Ghi chú</th>
            </tr>
          </thead>
          <tbody>
            @for (u of ungVienBang.data; track u.sinhVienId + '-' + u.ctdtId) {
              <tr>
                <td
                  [nzDisabled]="!u.coTheThem"
                  [nzChecked]="daChon().has(khoa(u))"
                  (nzCheckedChange)="chon(u, $event)"
                ></td>
                <td>{{ u.maSV }}</td>
                <td>{{ u.hoTen }}</td>
                <td>K{{ u.khoaHoc }}</td>
                <td>{{ u.maCTDT }}</td>
                <td>
                  @if (!u.coTheThem) {
                    <nz-tag nzColor="red">{{ u.lyDoKhongThem }}</nz-tag>
                  }
                  @if (u.hoSoTruocId) {
                    <nz-tag nzColor="purple">Làm lại (A21)</nz-tag>
                  }
                </td>
              </tr>
            }
          </tbody>
        </nz-table>
        <div class="thanh-cong-cu">
          <button
            nz-button
            nzType="primary"
            [disabled]="daChon().size === 0"
            [nzLoading]="dangThem()"
            (click)="themDaChon()"
          >
            Thêm {{ daChon().size }} sinh viên đã chọn
          </button>
          <button nz-button [disabled]="!coLoc()" [nzLoading]="dangThem()" (click)="themTheoLoc()">
            Thêm tất cả theo bộ lọc
          </button>
        </div>
      </ng-container>
    </nz-drawer>
  `,
  styles: `
    .thanh-cong-cu {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      margin: 12px 0;
    }
    .tien-do {
      margin-bottom: 16px;
      max-width: 720px;
    }
    .loi {
      color: #cf1322;
    }
    .loc {
      display: flex;
      flex-direction: column;
      gap: 8px;
      margin-bottom: 12px;
    }
    .loc nz-radio-group {
      display: flex;
      flex-wrap: wrap;
    }
    .loc-hang {
      display: flex;
      flex-wrap: wrap;
      gap: 12px;
      align-items: center;
    }
    .ket-qua {
      width: 220px;
    }
    .tim {
      width: 220px;
      max-width: 100%;
    }
    .so {
      text-align: right;
    }
    .ma {
      cursor: help;
      text-decoration: underline dotted;
    }
    .goi-y {
      color: rgba(0, 0, 0, 0.55);
      font-size: 13px;
      margin: 12px 0;
    }
  `,
})
export class TabSinhVienComponent implements OnInit {
  private readonly api = inject(DotDatnService);
  private readonly message = inject(NzMessageService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(AuthService);

  readonly dot = input.required<DotDatnItem>();
  /** Số hồ sơ thay đổi (thêm / xét) → trang cha cập nhật thông tin đợt. */
  readonly daThayDoi = output<void>();

  protected readonly ketQuaLoc = KET_QUA_LOC;
  protected readonly ketQua = nhanKetQua;
  protected readonly moTa = moTaNgoaiLe;
  protected readonly co = nhanCo;
  protected readonly trangThaiJob = nhanTrangThaiJob;
  protected readonly coQuyenDot = computed(() => this.auth.coQuyen(Quyen.DotQuanLy));
  protected readonly coQuyenXet = computed(() => this.auth.coQuyen(Quyen.XetDieuKienChay));

  // ----- danh sách hồ sơ
  protected readonly items = signal<HoSoXetGiaoItem[]>([]);
  protected readonly thongKe = signal<ThongKeTrangThaiItem[]>([]);
  protected readonly tongSo = computed(() => this.thongKe().reduce((t, x) => t + x.soLuong, 0));
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(20);
  protected readonly dangTai = signal(false);
  protected readonly trangThai = signal<TrangThaiSvTrongDot | null>(null);
  protected readonly ketQuaXet = signal<string | null>(null);
  protected readonly coNoTinChi = signal(false);
  protected readonly chuaQuaTTTN = signal(false);
  protected readonly q = signal('');
  protected readonly dangXem = signal<HoSoXetGiaoItem | null>(null);

  // ----- job xét giao
  protected readonly job = signal<XetGiaoJobItem | null>(null);
  protected readonly dangChay = computed(() => jobDangChay(this.job()?.trangThai));
  protected readonly phanTram = computed(() => {
    const j = this.job();
    if (!j) {
      return 0;
    }
    if (j.tongSo === 0) {
      return jobDangChay(j.trangThai) ? 0 : 100;
    }
    return Math.round((j.daXuLy * 100) / j.tongSo);
  });
  private theoDoi?: Subscription;

  // ----- thêm SV
  protected readonly themMo = signal(false);
  protected readonly locKhoa = signal<number | null>(null);
  protected readonly locNganh = signal('');
  protected readonly locQ = signal('');
  protected readonly coLoc = computed(
    () => !!this.locKhoa() || !!this.locNganh().trim() || !!this.locQ().trim(),
  );
  protected readonly ungVien = signal<SinhVienUngVienItem[]>([]);
  protected readonly daChon = signal<ReadonlySet<string>>(new Set());
  protected readonly chonTatCa = computed(() => {
    const chon = this.ungVien().filter((u) => u.coTheThem);
    return chon.length > 0 && chon.every((u) => this.daChon().has(this.khoa(u)));
  });
  protected readonly dangTimUngVien = signal(false);
  protected readonly dangThem = signal(false);

  private timer: ReturnType<typeof setTimeout> | undefined;

  ngOnInit(): void {
    this.taiLai();
    this.khoiPhucJob();
  }

  protected nhanTrangThai(t: string): string {
    return trangThaiStyle(t).label;
  }

  protected tim(value: string): void {
    this.q.set(value);
    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.taiLai(), 300);
  }

  protected doiTrang(params: NzTableQueryParams): void {
    if (params.pageIndex === this.page() && params.pageSize === this.pageSize()) {
      return;
    }
    this.page.set(params.pageIndex);
    this.pageSize.set(params.pageSize);
    this.tai();
  }

  /** Gọi từ trang cha khi tab khác làm đổi dữ liệu hồ sơ. */
  taiLai(): void {
    this.page.set(1);
    this.tai();
  }

  protected sauXetLai(): void {
    this.tai(true);
  }

  protected chayXet(): void {
    this.api
      .dotDatnChayXetGiao({ id: this.dot().id })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((j) => {
        this.job.set(j);
        this.theoDoiJob(j.jobId);
      });
  }

  // ----- thêm sinh viên

  protected moThem(): void {
    this.daChon.set(new Set());
    this.ungVien.set([]);
    this.themMo.set(true);
  }

  protected khoa(u: SinhVienUngVienItem): string {
    return `${u.sinhVienId}-${u.ctdtId}`;
  }

  protected chon(u: SinhVienUngVienItem, checked: boolean): void {
    const next = new Set(this.daChon());
    if (checked) {
      next.add(this.khoa(u));
    } else {
      next.delete(this.khoa(u));
    }
    this.daChon.set(next);
  }

  protected chonHet(checked: boolean): void {
    this.daChon.set(
      new Set(
        checked
          ? this.ungVien()
              .filter((u) => u.coTheThem)
              .map((u) => this.khoa(u))
          : [],
      ),
    );
  }

  protected timUngVien(): void {
    this.dangTimUngVien.set(true);
    this.api
      .dotDatnSinhVienUngVien({
        id: this.dot().id,
        khoaHoc: this.locKhoa() ?? undefined,
        nganh: this.locNganh().trim() || undefined,
        q: this.locQ().trim() || undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (rows) => {
          this.ungVien.set(rows);
          this.daChon.set(new Set());
          this.dangTimUngVien.set(false);
        },
        error: () => this.dangTimUngVien.set(false),
      });
  }

  protected themDaChon(): void {
    const body = this.ungVien()
      .filter((u) => this.daChon().has(this.khoa(u)))
      .map((u) => ({ sinhVienId: u.sinhVienId, ctdtId: u.ctdtId }));
    this.dangThem.set(true);
    this.api
      .dotDatnThemHoSo({ id: this.dot().id, body })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (kq) => this.sauKhiThem(kq.soDaThem, []),
        error: () => this.dangThem.set(false),
      });
  }

  protected themTheoLoc(): void {
    this.dangThem.set(true);
    this.api
      .dotDatnThemHoSoTheoLoc({
        id: this.dot().id,
        body: {
          khoaHoc: this.locKhoa(),
          nganh: this.locNganh().trim() || null,
          q: this.locQ().trim() || null,
        },
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (kq) =>
          this.sauKhiThem(
            kq.soDaThem,
            kq.boQua.map((b) => `${b.maSV}: ${b.lyDo}`),
          ),
        error: () => this.dangThem.set(false),
      });
  }

  private sauKhiThem(soDaThem: number, boQua: string[]): void {
    this.dangThem.set(false);
    this.themMo.set(false);
    this.message.success(`Đã thêm ${soDaThem} hồ sơ vào đợt.`);
    if (boQua.length > 0) {
      this.message.warning(`Bỏ qua ${boQua.length} sinh viên: ${boQua.join('; ')}`, {
        nzDuration: 8000,
      });
    }
    this.taiLai();
    this.daThayDoi.emit();
  }

  // ----- tải dữ liệu

  private tai(giuDangXem = false): void {
    this.dangTai.set(true);
    this.api
      .dotDatnHoSo({
        id: this.dot().id,
        trangThai: this.trangThai() ?? undefined,
        ketQuaXet: this.ketQuaXet() ?? undefined,
        coNoTinChi: this.coNoTinChi() || undefined,
        chuaQuaTTTN: this.chuaQuaTTTN() || undefined,
        q: this.q().trim() || undefined,
        page: this.page(),
        pageSize: this.pageSize(),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.items.set(res.items);
          this.total.set(res.total);
          this.thongKe.set(res.thongKe);
          this.dangTai.set(false);
          const xem = this.dangXem();
          if (giuDangXem && xem) {
            this.dangXem.set(res.items.find((x) => x.id === xem.id) ?? xem);
          }
        },
        error: () => this.dangTai.set(false),
      });
  }

  private khoiPhucJob(): void {
    if (!this.coQuyenXet()) {
      return;
    }
    this.api
      .dotDatnXetGiaoGanNhat({ id: this.dot().id })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((j) => {
        if (j) {
          this.job.set(j);
          if (jobDangChay(j.trangThai)) {
            this.theoDoiJob(j.jobId);
          }
        }
      });
  }

  /** Poll tiến độ mỗi giây tới khi job kết thúc, rồi tải lại danh sách. */
  private theoDoiJob(jobId: string): void {
    this.theoDoi?.unsubscribe();
    this.theoDoi = timer(1000, 1000)
      .pipe(
        switchMap(() => this.api.dotDatnTienDoXetGiao({ id: this.dot().id, jobId })),
        takeWhile((j) => jobDangChay(j.trangThai), true),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((j) => {
        this.job.set(j);
        if (!jobDangChay(j.trangThai)) {
          this.message.info(`Xét giao xong: ${j.daXuLy}/${j.tongSo} hồ sơ, ${j.soLoi} lỗi.`);
          this.tai();
          this.daThayDoi.emit();
        }
      });
  }
}
