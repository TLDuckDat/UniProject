import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzStepsModule } from 'ng-zorro-antd/steps';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzMessageService } from 'ng-zorro-antd/message';

import { ImportPreview, QuanTriApiService } from './quan-tri-api.service';

@Component({
  selector: 'app-import-page',
  standalone: true,
  imports: [
    FormsModule,
    NzButtonModule,
    NzCardModule,
    NzSelectModule,
    NzStepsModule,
    NzTableModule,
    NzTagModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nz-card nzTitle="Đồng bộ dữ liệu từ QLĐT / CTSV">
      <nz-steps [nzCurrent]="step()">
        <nz-step nzTitle="Chọn file" /><nz-step nzTitle="Xem trước lỗi" /><nz-step
          nzTitle="Xác nhận"
        />
      </nz-steps>
      @if (step() === 0) {
        <section class="step-body">
          <label
            >Loại dữ liệu
            <nz-select [ngModel]="loai()" (ngModelChange)="loai.set($event)">
              <nz-option nzValue="SinhVien" nzLabel="Sinh viên" /><nz-option
                nzValue="BangDiem"
                nzLabel="Bảng điểm"
              />
              <nz-option nzValue="KyLuatSV" nzLabel="Kỷ luật SV" /><nz-option
                nzValue="ThongTinTienDoSV"
                nzLabel="Thông tin tiến độ SV"
              />
            </nz-select>
          </label>
          <button nz-button (click)="taiMau()">Tải file mẫu CSV</button>
          <input
            type="file"
            accept=".csv,.xlsx"
            (change)="chonFile($event)"
            aria-label="Chọn file CSV hoặc Excel"
          />
          <button nz-button nzType="primary" [disabled]="!file() || loading()" (click)="xemTruoc()">
            Tải lên và xem trước
          </button>
        </section>
      }
      @if (step() === 1 && preview(); as p) {
        <section class="step-body">
          <p>
            <strong>{{ p.fileName }}</strong> · {{ p.totalRows }} dòng ·
            <nz-tag nzColor="green">{{ p.validRows }} hợp lệ</nz-tag>
            <nz-tag [nzColor]="p.errors.length ? 'red' : 'green'">{{ p.errors.length }} lỗi</nz-tag>
          </p>
          <nz-table #table [nzData]="p.errors" [nzPageSize]="10" nzSize="small">
            <thead>
              <tr>
                <th>Dòng</th>
                <th>Cột</th>
                <th>Lỗi</th>
              </tr>
            </thead>
            <tbody>
              @for (error of table.data; track $index) {
                <tr>
                  <td>{{ error.row }}</td>
                  <td>{{ error.field }}</td>
                  <td>{{ error.message }}</td>
                </tr>
              }
            </tbody>
          </nz-table>
          <div class="actions">
            <button nz-button (click)="step.set(0)">Quay lại</button
            ><button
              nz-button
              nzType="primary"
              [disabled]="p.validRows === 0 || loading()"
              (click)="xacNhan()"
            >
              Xác nhận ghi {{ p.validRows }} dòng hợp lệ
            </button>
          </div>
          <p class="hint">Các dòng lỗi sẽ bị bỏ qua. Dữ liệu chỉ được ghi sau khi xác nhận.</p>
        </section>
      }
      @if (step() === 2) {
        <section class="step-body">
          <nz-tag nzColor="green">Đã hoàn tất import</nz-tag>
          <p>{{ result() }}</p>
          <button nz-button nzType="primary" (click)="lamMoi()">Import file khác</button>
        </section>
      }
    </nz-card>
  `,
  styles: `
    :host {
      display: block;
      padding: 20px;
    }
    .step-body {
      display: grid;
      gap: 16px;
      padding-top: 24px;
    }
    .step-body label {
      display: grid;
      max-width: 400px;
      gap: 8px;
    }
    .step-body nz-select {
      width: 100%;
    }
    .actions {
      display: flex;
      gap: 8px;
    }
    .hint {
      color: #666;
      margin: 0;
    }
  `,
})
export class ImportPage {
  private readonly api = inject(QuanTriApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly message = inject(NzMessageService);
  readonly loai = signal('SinhVien');
  readonly step = signal(0);
  readonly file = signal<File | null>(null);
  readonly preview = signal<ImportPreview | null>(null);
  readonly result = signal('');
  readonly loading = signal(false);
  protected chonFile(event: Event): void {
    this.file.set((event.target as HTMLInputElement).files?.item(0) ?? null);
  }
  protected taiMau(): void {
    this.api
      .downloadTemplate(this.loai())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = `mau-${this.loai()}.csv`;
        anchor.click();
        URL.revokeObjectURL(url);
      });
  }
  protected xemTruoc(): void {
    const file = this.file();
    if (!file) return;
    this.loading.set(true);
    this.api
      .preview(this.loai(), file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.preview.set(result);
          this.step.set(1);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }
  protected xacNhan(): void {
    const preview = this.preview();
    if (!preview) return;
    this.loading.set(true);
    this.api
      .commit(preview.previewId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (r) => {
          this.result.set(
            `Đã thêm ${r.inserted}, cập nhật ${r.updated}, bỏ qua ${r.skipped} dòng.`,
          );
          this.step.set(2);
          this.loading.set(false);
          this.message.success('Đã ghi dữ liệu đồng bộ.');
        },
        error: () => this.loading.set(false),
      });
  }
  protected lamMoi(): void {
    this.step.set(0);
    this.file.set(null);
    this.preview.set(null);
    this.result.set('');
  }
}
