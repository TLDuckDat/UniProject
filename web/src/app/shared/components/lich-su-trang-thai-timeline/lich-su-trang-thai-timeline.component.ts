import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { NzTimelineModule } from 'ng-zorro-antd/timeline';

import { TrangThaiTagComponent } from '../trang-thai-tag/trang-thai-tag.component';
import { trangThaiStyle } from '../trang-thai-tag/trang-thai-style';

export interface LichSuTrangThaiItem {
  tuTrangThai: string | null;
  denTrangThai: string;
  lyDo: string | null;
  nguoiThucHienId: number | null;
  thoiDiem: string;
}

@Component({
  selector: 'app-lich-su-trang-thai-timeline',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, NzTimelineModule, TrangThaiTagComponent],
  templateUrl: './lich-su-trang-thai-timeline.component.html',
  styleUrl: './lich-su-trang-thai-timeline.component.scss',
})
export class LichSuTrangThaiTimelineComponent {
  readonly lichSu = input.required<readonly LichSuTrangThaiItem[]>();

  protected mau(trangThai: string): string {
    return trangThaiStyle(trangThai).color;
  }
}
