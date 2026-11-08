import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { AuthService } from '../../core/auth/auth.service';
import { CoQuyenDirective } from './co-quyen.directive';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CoQuyenDirective],
  template: `
    <button id="mo-dot" *coQuyen="'Dot.QuanLy'">Mở đợt</button>
    <button id="duyet" *coQuyen="['DeTai.Duyet', 'PhanCong.QuanLy']">Duyệt</button>
  `,
})
class HostComponent {}

describe('CoQuyenDirective', () => {
  const quyen = signal<string[]>([]);

  beforeEach(() => {
    quyen.set([]);
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        {
          provide: AuthService,
          useValue: { coQuyen: (...q: string[]) => q.some((x) => quyen().includes(x)) },
        },
      ],
    });
  });

  it('ẩn/hiện theo quyền và cập nhật khi quyền thay đổi', async () => {
    const fixture = TestBed.createComponent(HostComponent);
    const el = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
    expect(el.querySelector('#mo-dot')).toBeNull();
    expect(el.querySelector('#duyet')).toBeNull();

    quyen.set(['PhanCong.QuanLy']);
    await fixture.whenStable();
    expect(el.querySelector('#mo-dot')).toBeNull();
    expect(el.querySelector('#duyet')).not.toBeNull();

    quyen.set(['Dot.QuanLy']);
    await fixture.whenStable();
    expect(el.querySelector('#mo-dot')).not.toBeNull();
    expect(el.querySelector('#duyet')).toBeNull();
  });
});
