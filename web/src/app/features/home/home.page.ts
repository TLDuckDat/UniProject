import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDescriptionsModule } from 'ng-zorro-antd/descriptions';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzResultModule } from 'ng-zorro-antd/result';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { catchError, map, of, startWith, switchMap } from 'rxjs';

import { PingResponse } from '../../api/models';
import { SystemService } from '../../api/services';

type PingState = { status: 'loading' } | { status: 'ok'; data: PingResponse } | { status: 'error' };

@Component({
  selector: 'app-home-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    NzButtonModule,
    NzCardModule,
    NzDescriptionsModule,
    NzIconModule,
    NzResultModule,
    NzSpinModule,
    NzTagModule,
  ],
  templateUrl: './home.page.html',
  styleUrl: './home.page.scss',
})
export class HomePage {
  private readonly system = inject(SystemService);
  private readonly reloadCount = signal(0);

  /** Trạng thái gọi GET /api/v1/ping; gọi lại mỗi khi `reloadCount` thay đổi. */
  protected readonly ping = toSignal(
    toObservable(this.reloadCount).pipe(
      switchMap(() =>
        this.system.ping().pipe(
          map((data): PingState => ({ status: 'ok', data })),
          // Interceptor đã hiển thị nz-message; ở đây chỉ chuyển sang trạng thái lỗi.
          catchError(() => of<PingState>({ status: 'error' })),
          startWith<PingState>({ status: 'loading' }),
        ),
      ),
    ),
    { initialValue: { status: 'loading' } as PingState },
  );

  protected reload(): void {
    this.reloadCount.update((n) => n + 1);
  }
}
