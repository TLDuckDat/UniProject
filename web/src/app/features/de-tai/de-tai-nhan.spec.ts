import { nhanMucTai, nhanTrangThaiDeTai, tyLeTai } from './de-tai-nhan';

describe('de-tai-nhan (đề tài & phân công)', () => {
  it('B03: tải GV đổi màu theo mức quota (thấp/cao/đủ/vượt)', () => {
    expect(nhanMucTai('Thap').color).toBe('green');
    expect(nhanMucTai('Cao').color).toBe('orange');
    expect(nhanMucTai('DuQuota').color).toBe('blue');
    expect(nhanMucTai('VuotQuota')).toEqual({ label: 'Vượt quota', color: 'red' });
  });

  it('B03: thanh tiến độ tải tối đa 100% và xử lý quota = 0', () => {
    expect(tyLeTai(3, 5)).toBe(60);
    expect(tyLeTai(7, 5)).toBe(100);
    expect(tyLeTai(0, 0)).toBe(0);
    expect(tyLeTai(1, 0)).toBe(100);
  });

  it('B04: trạng thái đề xuất của SV chờ GV xác nhận trước khi Bộ môn duyệt', () => {
    expect(nhanTrangThaiDeTai('Nhap').label).toBe('Chờ GV xác nhận');
    expect(nhanTrangThaiDeTai('ChoDuyet').label).toBe('Chờ Bộ môn duyệt');
    expect(nhanTrangThaiDeTai('DaDuyet').color).toBe('green');
  });
});
