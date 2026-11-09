import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { NzTagModule } from 'ng-zorro-antd/tag';

import { trangThaiStyle } from './trang-thai-style';

@Component({
  selector: 'app-trang-thai-tag',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NzTagModule],
  template: '<nz-tag [nzColor]="style().color">{{ style().label }}</nz-tag>',
})
export class TrangThaiTagComponent {
  readonly trangThai = input.required<string>();
  protected readonly style = computed(() => trangThaiStyle(this.trangThai()));
}
