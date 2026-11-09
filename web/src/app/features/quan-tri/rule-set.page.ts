import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormControl,
  FormGroup,
  FormsModule,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTagModule } from 'ng-zorro-antd/tag';

import {
  RuleSetCohort,
  RuleSetItem,
  RuleSetRequest,
  ThamSoBoMonRequest,
  ThamSoDaoVanRequest,
  QuanTriApiService,
} from './quan-tri-api.service';

interface RuleSetControls {
  maRuleSet: FormControl<string>;
  tenRuleSet: FormControl<string>;
  canCuPhapLy: FormControl<string>;
  maxTCChuaTichLuy: FormControl<number>;
  gpaBaoVeMin: FormControl<number>;
  gpaTotNghiepMin: FormControl<number>;
  diemDatDATN: FormControl<number>;
  trongSoQT: FormControl<number>;
  heSoThoiGianToiDa: FormControl<number>;
  minTVHoiDong: FormControl<number>;
  maxTVHoiDong: FormControl<number>;
  nguongHaHangHocLaiPct: FormControl<number>;
  haHangKhiKyLuat: FormControl<boolean>;
  hieuLucTu: FormControl<string>;
  hieuLucDen: FormControl<string>;
}
interface CohortControls {
  ranges: FormControl<string>;
}
interface DepartmentControls {
  boMonId: FormControl<number>;
  ruleSetId: FormControl<number>;
  trongSoQT: FormControl<number>;
  trongSoHD: FormControl<number>;
  tachDiemKhac: FormControl<boolean>;
  soNgaySuaGapToiDa: FormControl<number>;
  dungHanKhiNghiTamThoi: FormControl<boolean>;
}
interface PlagiarismControls {
  khoaId: FormControl<number>;
  ruleSetId: FormControl<number>;
  chuongTrinh: FormControl<string>;
  bat: FormControl<boolean>;
  nguongPhanTram: FormControl<number>;
}

@Component({
  selector: 'app-rule-set-page',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    NzButtonModule,
    NzCardModule,
    NzDrawerModule,
    NzFormModule,
    NzInputModule,
    NzSelectModule,
    NzTagModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="page-head">
      <h1>RuleSet</h1>
      <button nz-button nzType="primary" (click)="moForm()">Tạo phiên bản</button>
    </section>
    <div class="compare-select">
      <label
        >Phiên bản A
        <nz-select [ngModel]="leftId()" (ngModelChange)="chonTrai($event)">
          @for (r of ruleSets(); track r.id) {
            <nz-option [nzValue]="r.id" [nzLabel]="r.maRuleSet + ' · ' + r.tenRuleSet" />
          }</nz-select
      ></label>
      <label
        >Phiên bản B
        <nz-select [ngModel]="rightId()" (ngModelChange)="chonPhai($event)">
          @for (r of ruleSets(); track r.id) {
            <nz-option [nzValue]="r.id" [nzLabel]="r.maRuleSet + ' · ' + r.tenRuleSet" />
          }</nz-select
      ></label>
    </div>
    <div class="compare-grid">
      @for (item of [left(), right()]; track $index) {
        @if (item) {
          <nz-card [nzTitle]="item.maRuleSet + ' · ' + item.tenRuleSet">
            <p>{{ item.canCuPhapLy || 'Chưa khai căn cứ pháp lý' }}</p>
            <nz-tag nzColor="orange"
              >⚠ O1 · Trọng số QT {{ item.trongSoQT }} / HĐ {{ 1 - item.trongSoQT }} — cần xác nhận
              phương án 30/70 hay 40/60</nz-tag
            >
            @if (hasK66(item.id)) {
              <nz-tag nzColor="orange">⚠ O2 · Khóa 66 cần xác nhận</nz-tag>
            }
            <nz-tag [nzColor]="item.daSuDung ? 'default' : 'green'">{{
              item.daSuDung ? 'Đã dùng · khóa sửa' : 'Chưa sử dụng · có thể sửa'
            }}</nz-tag>
            <dl>
              <dt>TC chưa tích lũy tối đa</dt>
              <dd>{{ item.maxTCChuaTichLuy }}</dd>
              <dt>GPA bảo vệ / tốt nghiệp</dt>
              <dd>{{ item.gpaBaoVeMin }} / {{ item.gpaTotNghiepMin }}</dd>
              <dt>Điểm ĐATN tối thiểu</dt>
              <dd>{{ item.diemDatDATN }}</dd>
              <dt>Số thành viên hội đồng</dt>
              <dd>{{ item.minTVHoiDong }}–{{ item.maxTVHoiDong }}</dd>
              <dt>Hiệu lực</dt>
              <dd>{{ item.hieuLucTu }} – {{ item.hieuLucDen || 'không giới hạn' }}</dd>
              <dt>Khóa học được gán</dt>
              <dd>{{ cohortLabel(item.id) }}</dd>
            </dl>
            <div class="actions">
              <button nz-button (click)="moForm(item)" [disabled]="item.daSuDung">Sửa</button>
              <button nz-button nzType="primary" nzGhost (click)="nhanBan(item)">Nhân bản</button>
            </div>
          </nz-card>
        }
      }
    </div>
    <div class="config-grid">
      <nz-card nzTitle="Gán khóa học · ⚠ O2">
        <p>
          Mỗi dòng là một khoảng: <code>64-65</code>, <code>66-66!</code>, <code>67-*</code>. Dấu !
          đánh dấu cần xác nhận.
        </p>
        <form [formGroup]="cohortForm" (ngSubmit)="luuKhoaHoc()">
          <textarea
            nz-input
            rows="5"
            formControlName="ranges"
            aria-label="Các khoảng khóa học"
          ></textarea>
          <button nz-button nzType="primary" [disabled]="!leftId() || !!left()?.daSuDung">
            Lưu khoảng khóa học cho {{ left()?.maRuleSet }}
          </button>
        </form>
      </nz-card>
      <nz-card nzTitle="Tham số bộ môn · ⚠ O1 / O4">
        <form nz-form [formGroup]="departmentForm" (ngSubmit)="luuThamSoBoMon()">
          <nz-form-item
            ><nz-form-label>ID bộ môn</nz-form-label
            ><nz-form-control
              ><input nz-input type="number" formControlName="boMonId" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>RuleSet</nz-form-label
            ><nz-form-control
              ><nz-select formControlName="ruleSetId">
                @for (r of ruleSets(); track r.id) {
                  <nz-option [nzValue]="r.id" [nzLabel]="r.maRuleSet" />
                }</nz-select></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Trọng số QT</nz-form-label
            ><nz-form-control
              ><input
                nz-input
                type="number"
                step="0.01"
                formControlName="trongSoQT" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Trọng số HĐ</nz-form-label
            ><nz-form-control
              ><input
                nz-input
                type="number"
                step="0.01"
                formControlName="trongSoHD" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Số ngày sửa gấp (1–3)</nz-form-label
            ><nz-form-control
              ><input nz-input type="number" formControlName="soNgaySuaGapToiDa" /></nz-form-control
          ></nz-form-item>
          <label><input type="checkbox" formControlName="tachDiemKhac" /> Tách điểm khác</label
          ><br />
          <label
            ><input type="checkbox" formControlName="dungHanKhiNghiTamThoi" /> Dừng hạn khi nghỉ tạm
            thời (O4)</label
          ><br />
          <button nz-button type="button" (click)="taiThamSoBoMon()">Tải cài đặt hiện có</button>
          <button nz-button nzType="primary" [disabled]="!!left()?.daSuDung">
            Lưu tham số bộ môn
          </button>
        </form>
      </nz-card>
      <nz-card nzTitle="Tham số đạo văn theo Khoa × Chương trình">
        <form nz-form [formGroup]="plagiarismForm" (ngSubmit)="luuThamSoDaoVan()">
          <nz-form-item
            ><nz-form-label>ID khoa</nz-form-label
            ><nz-form-control
              ><input nz-input type="number" formControlName="khoaId" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>RuleSet ID (0 = mặc định)</nz-form-label
            ><nz-form-control
              ><input nz-input type="number" formControlName="ruleSetId" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Chương trình</nz-form-label
            ><nz-form-control
              ><nz-select formControlName="chuongTrinh"
                ><nz-option nzValue="DaiTra" nzLabel="Đại trà" /><nz-option
                  nzValue="CLC"
                  nzLabel="CLC" /><nz-option
                  nzValue="TienTien"
                  nzLabel="Tiên tiến" /></nz-select></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Ngưỡng (⚠ O2, 20–25%)</nz-form-label
            ><nz-form-control
              ><input
                nz-input
                type="number"
                step="0.1"
                formControlName="nguongPhanTram" /></nz-form-control
          ></nz-form-item>
          <label><input type="checkbox" formControlName="bat" /> Bật kiểm tra đạo văn</label><br />
          <button nz-button type="button" (click)="taiThamSoDaoVan()">Tải cài đặt hiện có</button>
          <button nz-button nzType="primary" [disabled]="!!left()?.daSuDung">
            Lưu tham số đạo văn
          </button>
        </form>
      </nz-card>
    </div>
    <nz-drawer
      [nzVisible]="drawer()"
      [nzTitle]="editing() ? 'Sửa RuleSet' : 'Tạo RuleSet'"
      [nzWidth]="600"
      (nzOnClose)="drawer.set(false)"
    >
      <ng-container *nzDrawerContent>
        <form nz-form [formGroup]="form" (ngSubmit)="luu()">
          <nz-form-item
            ><nz-form-label nzRequired>Mã</nz-form-label
            ><nz-form-control><input nz-input formControlName="maRuleSet" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label nzRequired>Tên</nz-form-label
            ><nz-form-control><input nz-input formControlName="tenRuleSet" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Căn cứ pháp lý</nz-form-label
            ><nz-form-control><input nz-input formControlName="canCuPhapLy" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>TC chưa tích lũy tối đa</nz-form-label
            ><nz-form-control
              ><input nz-input type="number" formControlName="maxTCChuaTichLuy" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>GPA bảo vệ tối thiểu</nz-form-label
            ><nz-form-control
              ><input
                nz-input
                type="number"
                step="0.01"
                formControlName="gpaBaoVeMin" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>GPA tốt nghiệp tối thiểu</nz-form-label
            ><nz-form-control
              ><input
                nz-input
                type="number"
                step="0.01"
                formControlName="gpaTotNghiepMin" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Điểm ĐATN đạt</nz-form-label
            ><nz-form-control
              ><input
                nz-input
                type="number"
                step="0.1"
                formControlName="diemDatDATN" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Trọng số QT (⚠ O1)</nz-form-label
            ><nz-form-control
              ><input
                nz-input
                type="number"
                step="0.01"
                formControlName="trongSoQT" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Hệ số thời gian tối đa</nz-form-label
            ><nz-form-control
              ><input
                nz-input
                type="number"
                step="0.1"
                formControlName="heSoThoiGianToiDa" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Thành viên hội đồng tối thiểu</nz-form-label
            ><nz-form-control
              ><input nz-input type="number" formControlName="minTVHoiDong" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Thành viên hội đồng tối đa</nz-form-label
            ><nz-form-control
              ><input nz-input type="number" formControlName="maxTVHoiDong" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Ngưỡng hạ hạng học lại (%)</nz-form-label
            ><nz-form-control
              ><input
                nz-input
                type="number"
                step="0.1"
                formControlName="nguongHaHangHocLaiPct" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Hiệu lực từ</nz-form-label
            ><nz-form-control
              ><input nz-input type="date" formControlName="hieuLucTu" /></nz-form-control
          ></nz-form-item>
          <nz-form-item
            ><nz-form-label>Hiệu lực đến</nz-form-label
            ><nz-form-control
              ><input nz-input type="date" formControlName="hieuLucDen" /></nz-form-control
          ></nz-form-item>
          <button nz-button nzType="primary" [disabled]="form.invalid">Lưu</button>
        </form>
      </ng-container>
    </nz-drawer>
  `,
  styles: `
    :host {
      display: block;
      padding: 20px;
    }
    .page-head,
    .compare-select,
    .actions {
      display: flex;
      gap: 12px;
      align-items: center;
    }
    .page-head h1 {
      margin-right: auto;
    }
    .compare-select {
      margin-bottom: 16px;
    }
    .compare-select label {
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .compare-select nz-select {
      min-width: 280px;
    }
    .compare-grid,
    .config-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 16px;
      margin-bottom: 16px;
    }
    .config-grid nz-card:last-child {
      grid-column: span 2;
    }
    dl {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 6px 16px;
      margin: 16px 0;
    }
    dt {
      color: #666;
    }
    dd {
      margin: 0;
    }
    form {
      margin-top: 12px;
    }
    @media (max-width: 800px) {
      .compare-grid,
      .config-grid {
        grid-template-columns: 1fr;
      }
      .config-grid nz-card:last-child {
        grid-column: auto;
      }
      .compare-select {
        align-items: stretch;
        flex-direction: column;
      }
    }
  `,
})
export class RuleSetPage {
  private readonly api = inject(QuanTriApiService);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly message = inject(NzMessageService);
  readonly ruleSets = signal<RuleSetItem[]>([]);
  readonly leftId = signal<number | null>(null);
  readonly rightId = signal<number | null>(null);
  readonly cohorts = signal<Record<number, RuleSetCohort[]>>({});
  readonly drawer = signal(false);
  readonly editing = signal<number | null>(null);
  readonly left = () => this.ruleSets().find((x) => x.id === this.leftId()) ?? null;
  readonly right = () => this.ruleSets().find((x) => x.id === this.rightId()) ?? null;
  readonly form: FormGroup<RuleSetControls> = this.fb.group({
    maRuleSet: this.fb.control('', Validators.required),
    tenRuleSet: this.fb.control('', Validators.required),
    canCuPhapLy: this.fb.control(''),
    maxTCChuaTichLuy: this.fb.control(10),
    gpaBaoVeMin: this.fb.control(2),
    gpaTotNghiepMin: this.fb.control(2),
    diemDatDATN: this.fb.control(5.5),
    trongSoQT: this.fb.control(0.3),
    heSoThoiGianToiDa: this.fb.control(2),
    minTVHoiDong: this.fb.control(3),
    maxTVHoiDong: this.fb.control(5),
    nguongHaHangHocLaiPct: this.fb.control(0),
    haHangKhiKyLuat: this.fb.control(false),
    hieuLucTu: this.fb.control('2026-01-01'),
    hieuLucDen: this.fb.control(''),
  });
  readonly cohortForm: FormGroup<CohortControls> = this.fb.group({ ranges: this.fb.control('') });
  readonly departmentForm: FormGroup<DepartmentControls> = this.fb.group({
    boMonId: this.fb.control(0),
    ruleSetId: this.fb.control(0),
    trongSoQT: this.fb.control(0.3),
    trongSoHD: this.fb.control(0.7),
    tachDiemKhac: this.fb.control(false),
    soNgaySuaGapToiDa: this.fb.control(1),
    dungHanKhiNghiTamThoi: this.fb.control(false),
  });
  readonly plagiarismForm: FormGroup<PlagiarismControls> = this.fb.group({
    khoaId: this.fb.control(0),
    ruleSetId: this.fb.control(0),
    chuongTrinh: this.fb.control('DaiTra'),
    bat: this.fb.control(false),
    nguongPhanTram: this.fb.control(20),
  });
  constructor() {
    this.tai();
  }
  protected tai(): void {
    this.api
      .ruleSets()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((page) => {
        this.ruleSets.set(page.items);
        if (page.items.length) {
          this.chonTrai(page.items[0].id);
          this.chonPhai(page.items[1]?.id ?? page.items[0].id);
          page.items.forEach((x) => this.taiKhoaHoc(x.id));
        }
      });
  }
  protected chonTrai(id: number): void {
    this.leftId.set(id);
    this.taiKhoaHoc(id);
    this.departmentForm.controls.ruleSetId.setValue(id);
    this.plagiarismForm.controls.ruleSetId.setValue(id);
  }
  protected chonPhai(id: number): void {
    this.rightId.set(id);
    this.taiKhoaHoc(id);
  }
  protected hasK66(id: number): boolean {
    return (
      this.cohorts()[id]?.some(
        (x) => x.khoaHocTu <= 66 && (x.khoaHocDen === null || x.khoaHocDen >= 66),
      ) ?? false
    );
  }
  protected cohortLabel(id: number): string {
    return (
      (this.cohorts()[id] ?? [])
        .map((x) => `${x.khoaHocTu}–${x.khoaHocDen ?? '∞'}${x.canXacNhan ? ' ⚠' : ''}`)
        .join(', ') || 'Chưa gán'
    );
  }
  protected luuKhoaHoc(): void {
    const id = this.leftId();
    if (!id) return;
    const rows: RuleSetCohort[] = [];
    for (const raw of this.cohortForm.controls.ranges.value
      .split('\n')
      .map((x) => x.trim())
      .filter(Boolean)) {
      const m = /^(\d+)\s*-\s*(\d+|\*)(!)?$/.exec(raw);
      if (!m) {
        this.message.error(`Khoảng khóa học không hợp lệ: ${raw}`);
        return;
      }
      rows.push({
        ruleSetId: id,
        khoaHocTu: Number(m[1]),
        khoaHocDen: m[2] === '*' ? null : Number(m[2]),
        canXacNhan: !!m[3],
      });
    }
    this.api
      .saveRuleSetCohorts(id, rows)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.message.success('Đã gán khóa học.');
          this.taiKhoaHoc(id);
        },
      });
  }
  protected luuThamSoBoMon(): void {
    const body = this.departmentForm.getRawValue() as ThamSoBoMonRequest;
    this.api
      .saveThamSoBoMon(body)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: () => this.message.success('Đã lưu tham số bộ môn.') });
  }
  protected taiThamSoBoMon(): void {
    const { boMonId, ruleSetId } = this.departmentForm.getRawValue();
    if (!boMonId || !ruleSetId) return;
    this.api
      .thamSoBoMon(ruleSetId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((rows) => {
        const item = rows.find((x) => x.boMonId === boMonId);
        if (!item) {
          this.message.info('Chưa có tham số cho cặp bộ môn/RuleSet này.');
          return;
        }
        this.departmentForm.patchValue(item);
      });
  }
  protected luuThamSoDaoVan(): void {
    const value = this.plagiarismForm.getRawValue();
    const body: ThamSoDaoVanRequest = { ...value, ruleSetId: value.ruleSetId || null };
    this.api
      .saveThamSoDaoVan(body)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: () => this.message.success('Đã lưu tham số đạo văn.') });
  }
  protected taiThamSoDaoVan(): void {
    const { khoaId, chuongTrinh } = this.plagiarismForm.getRawValue();
    if (!khoaId) return;
    this.api
      .thamSoDaoVan(khoaId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((rows) => {
        const item = rows.find((x) => x.chuongTrinh === chuongTrinh);
        if (!item) {
          this.message.info('Chưa có tham số đạo văn cho khoa/chương trình này.');
          return;
        }
        this.plagiarismForm.patchValue({
          ruleSetId: item.ruleSetId ?? 0,
          bat: item.bat,
          nguongPhanTram: item.nguongPhanTram,
        });
      });
  }
  protected moForm(item?: RuleSetItem): void {
    if (item?.daSuDung) return;
    this.editing.set(item?.id ?? null);
    this.form.reset({
      maRuleSet: item?.maRuleSet ?? '',
      tenRuleSet: item?.tenRuleSet ?? '',
      canCuPhapLy: item?.canCuPhapLy ?? '',
      maxTCChuaTichLuy: item?.maxTCChuaTichLuy ?? 10,
      gpaBaoVeMin: item?.gpaBaoVeMin ?? 2,
      gpaTotNghiepMin: item?.gpaTotNghiepMin ?? 2,
      diemDatDATN: item?.diemDatDATN ?? 5.5,
      trongSoQT: item?.trongSoQT ?? 0.3,
      heSoThoiGianToiDa: item?.heSoThoiGianToiDa ?? 2,
      minTVHoiDong: item?.minTVHoiDong ?? 3,
      maxTVHoiDong: item?.maxTVHoiDong ?? 5,
      nguongHaHangHocLaiPct: item?.nguongHaHangHocLaiPct ?? 0,
      haHangKhiKyLuat: item?.haHangKhiKyLuat ?? false,
      hieuLucTu: item?.hieuLucTu ?? '2026-01-01',
      hieuLucDen: item?.hieuLucDen ?? '',
    });
    this.drawer.set(true);
  }
  protected luu(): void {
    const value = this.form.getRawValue();
    const body: RuleSetRequest = { ...value, hieuLucDen: value.hieuLucDen || null };
    this.api
      .saveRuleSet(this.editing(), body)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.drawer.set(false);
          this.message.success('Đã lưu RuleSet.');
          this.tai();
          this.leftId.set(result.id);
        },
      });
  }
  protected nhanBan(item: RuleSetItem): void {
    this.api
      .cloneRuleSet(item.id, `${item.maRuleSet}-V2`, `${item.tenRuleSet} (bản sao)`)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.message.success('Đã tạo phiên bản mới.');
          this.tai();
          this.leftId.set(result.id);
        },
      });
  }
  private taiKhoaHoc(id: number): void {
    this.api
      .ruleSetCohorts(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((rows) => {
        this.cohorts.update((current) => ({ ...current, [id]: rows }));
        if (this.leftId() === id)
          this.cohortForm.controls.ranges.setValue(
            rows
              .map((x) => `${x.khoaHocTu}-${x.khoaHocDen ?? '*'}${x.canXacNhan ? '!' : ''}`)
              .join('\n'),
          );
      });
  }
}
