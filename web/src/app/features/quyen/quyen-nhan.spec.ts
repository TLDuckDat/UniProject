import { hashRutGon, mauHanDoc, nhanHanDoc, nhanKetQuaDocDuyet, nhanLoaiBan } from './quyen-nhan';

describe('quyen-nhan (nộp & đọc quyển)', () => {
  it('hạn đọc: quá hạn đỏ, sắp đến hạn cam, còn lại xanh', () => {
    expect(mauHanDoc(-2)).toBe('red');
    expect(mauHanDoc(1)).toBe('orange');
    expect(mauHanDoc(5)).toBe('green');
    expect(nhanHanDoc(-2)).toBe('Quá hạn 2 ngày');
    expect(nhanHanDoc(0)).toBe('Đến hạn hôm nay');
  });

  it('kết luận đọc duyệt và loại bản hiển thị đúng tiếng Việt', () => {
    expect(nhanKetQuaDocDuyet('DongY').color).toBe('green');
    expect(nhanKetQuaDocDuyet('SuaGap').label).toBe('Yêu cầu sửa gấp');
    expect(nhanKetQuaDocDuyet('KhongDongY').color).toBe('red');
    expect(nhanLoaiBan('BanSuaGap')).toBe('Bản sửa gấp');
  });

  it('hash được rút gọn để hiển thị (đầy đủ ở tooltip)', () => {
    const hash = 'a'.repeat(32) + 'b'.repeat(32);
    expect(hashRutGon(hash)).toBe('aaaaaaaa…bbbb');
  });
});
