import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzLayoutModule } from 'ng-zorro-antd/layout';

/** Khung trang tối giản; layout đầy đủ (menu, người dùng) làm ở Phần 3. */
@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, NzLayoutModule, NzIconModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly appName = 'UPM – Quản lý Đồ án tốt nghiệp';
  protected readonly year = new Date().getFullYear();
}
