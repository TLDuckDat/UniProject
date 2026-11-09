import { TestBed } from '@angular/core/testing';

import { TrangThaiTagComponent } from './trang-thai-tag.component';
import { trangThaiStyle } from './trang-thai-style';

describe('TrangThaiTagComponent', () => {
  it('hiển thị nhãn và màu theo nhóm trạng thái', async () => {
    TestBed.configureTestingModule({ imports: [TrangThaiTagComponent] });
    const fixture = TestBed.createComponent(TrangThaiTagComponent);
    fixture.componentRef.setInput('trangThai', 'PHAI_LAM_LAI');
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Phải làm lại');
    expect(trangThaiStyle('PHAI_LAM_LAI').color).toBe('red');
    expect(trangThaiStyle('CHO_BAN_CUOI').color).toBe('orange');
    expect(trangThaiStyle('DA_GIAO').color).toBe('blue');
    expect(trangThaiStyle('HOAN_TAT').color).toBe('green');
  });
});
