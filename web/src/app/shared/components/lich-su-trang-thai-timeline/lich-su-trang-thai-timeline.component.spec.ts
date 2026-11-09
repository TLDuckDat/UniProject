import { TestBed } from '@angular/core/testing';

import { LichSuTrangThaiTimelineComponent } from './lich-su-trang-thai-timeline.component';

describe('LichSuTrangThaiTimelineComponent', () => {
  it('hiển thị trạng thái, thời điểm và lý do trong lịch sử', async () => {
    TestBed.configureTestingModule({ imports: [LichSuTrangThaiTimelineComponent] });
    const fixture = TestBed.createComponent(LichSuTrangThaiTimelineComponent);
    fixture.componentRef.setInput('lichSu', [
      {
        tuTrangThai: 'DA_GIAO',
        denTrangThai: 'DANG_THUC_HIEN',
        lyDo: 'Bắt đầu đợt đồ án.',
        nguoiThucHienId: null,
        thoiDiem: '2026-11-08T12:00:00Z',
      },
    ]);
    await fixture.whenStable();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Đang thực hiện');
    expect(text).toContain('Bắt đầu đợt đồ án.');
    expect(text).toContain('Hệ thống');
  });
});
