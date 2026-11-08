import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzResultModule } from 'ng-zorro-antd/result';

@Component({
  selector: 'app-forbidden-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, NzButtonModule, NzResultModule],
  template: `
    <nz-result nzStatus="403" nzTitle="403" nzSubTitle="Bạn không có quyền truy cập trang này.">
      <div nz-result-extra>
        <a nz-button nzType="primary" routerLink="/">Về trang chủ</a>
      </div>
    </nz-result>
  `,
})
export class ForbiddenPage {}

@Component({
  selector: 'app-not-found-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, NzButtonModule, NzResultModule],
  template: `
    <nz-result nzStatus="404" nzTitle="404" nzSubTitle="Không tìm thấy trang bạn yêu cầu.">
      <div nz-result-extra>
        <a nz-button nzType="primary" routerLink="/">Về trang chủ</a>
      </div>
    </nz-result>
  `,
})
export class NotFoundPage {}

/** Trang tạm cho các module nghiệp vụ chưa làm (thay ở các phần sau). */
@Component({
  selector: 'app-dang-xay-dung-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NzResultModule],
  template: `
    <nz-result
      nzStatus="info"
      nzTitle="Đang xây dựng"
      nzSubTitle="Chức năng này sẽ có trong các phần tiếp theo."
    />
  `,
})
export class DangXayDungPage {}
