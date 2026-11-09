import { ngayVn, nhanTrangThaiMoc, tenMoc } from './tien-do-nhan';

describe('tien-do-nhan (tiến độ 3 mốc)', () => {
  it('C01/C02: quá hạn chưa nộp và dưới yêu cầu có màu cảnh báo khác nhau', () => {
    expect(nhanTrangThaiMoc('KhongNop').color).toBe('red');
    expect(nhanTrangThaiMoc('CanhBao').color).toBe('orange');
    expect(nhanTrangThaiMoc('Dat').color).toBe('green');
    expect(nhanTrangThaiMoc(null).label).toBe('Chưa đến hạn');
  });

  it('hiển thị tên mốc theo tuần và ngày dạng dd/MM/yyyy', () => {
    expect(tenMoc('M2')).toContain('tuần 8–9');
    expect(ngayVn('2026-10-04')).toBe('04/10/2026');
    expect(ngayVn(null)).toBe('—');
  });
});
