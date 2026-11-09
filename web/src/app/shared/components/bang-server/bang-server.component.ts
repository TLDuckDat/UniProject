import { ChangeDetectionStrategy, Component, effect, input, output } from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { FormsModule } from '@angular/forms';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzTableModule, NzTableQueryParams } from 'ng-zorro-antd/table';

export interface CotBang {
  key: string;
  label: string;
  sortable?: boolean;
}
export interface TruyVanBang {
  page: number;
  pageSize: number;
  sort?: string;
  desc: boolean;
  q: string;
}

@Component({
  selector: 'app-bang-server',
  standalone: true,
  imports: [FormsModule, NzButtonModule, NzInputModule, NzTableModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <input
      nz-input
      class="search"
      placeholder="Tìm kiếm"
      [(ngModel)]="search"
      (ngModelChange)="tim($event)"
    />
    <nz-table
      #table
      nzShowSizeChanger
      [nzData]="rows()"
      [nzFrontPagination]="false"
      [nzTotal]="total()"
      [nzPageIndex]="page()"
      [nzPageSize]="pageSize()"
      [nzLoading]="loading()"
      (nzQueryParams)="truyVanBang($event)"
    >
      <thead>
        <tr>
          @for (cot of columns(); track cot.key) {
            <th [nzColumnKey]="cot.key" [nzSortFn]="cot.sortable ? true : null">{{ cot.label }}</th>
          }
          <th>Thao tác</th>
        </tr>
      </thead>
      <tbody>
        @for (row of table.data; track row['id']) {
          <tr>
            @for (cot of columns(); track cot.key) {
              <td>{{ row[cot.key] ?? '—' }}</td>
            }
            <td>
              <button nz-button nzType="link" (click)="sua(row)">Sửa</button
              ><button nz-button nzType="link" nzDanger (click)="xoa(row)">Xóa</button>
            </td>
          </tr>
        }
      </tbody>
    </nz-table>
  `,
  styles: `
    .search {
      width: min(360px, 100%);
      margin-bottom: 12px;
    }
  `,
})
export class BangServerComponent {
  readonly columns = input.required<CotBang[]>();
  readonly rows = input.required<Record<string, unknown>[]>();
  readonly total = input(0);
  readonly page = input(1);
  readonly pageSize = input(20);
  readonly loading = input(false);
  readonly resetToken = input<string | number>();
  readonly truyVan = output<TruyVanBang>();
  readonly suaBanGhi = output<Record<string, unknown>>();
  readonly xoaBanGhi = output<Record<string, unknown>>();
  protected search = '';

  constructor() {
    effect(() => {
      this.resetToken();
      this.search = '';
    });
  }

  protected tim(q: string): void {
    this.truyVan.emit({ page: 1, pageSize: this.pageSize(), desc: false, q });
  }
  protected sua(row: Record<string, unknown>): void {
    this.suaBanGhi.emit(row);
  }
  protected xoa(row: Record<string, unknown>): void {
    this.xoaBanGhi.emit(row);
  }
  protected truyVanBang(params: NzTableQueryParams): void {
    const sort = params.sort.find((item) => item.value !== null);
    this.truyVan.emit({
      page: params.pageIndex,
      pageSize: params.pageSize,
      sort: sort?.key,
      desc: sort?.value === 'descend',
      q: this.search,
    });
  }
}
