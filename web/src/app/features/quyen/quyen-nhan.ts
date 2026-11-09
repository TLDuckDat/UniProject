/** Nhãn và tiện ích cho nộp quyển / đọc duyệt (UC-3b). */

export function nhanLoaiBan(loai: string): string {
  switch (loai) {
    case 'NopQuyen':
      return 'Nộp quyển';
    case 'BanSuaGap':
      return 'Bản sửa gấp';
    case 'BanSuaSauBV':
      return 'Bản sửa sau bảo vệ';
    case 'BanCuoi':
      return 'Bản cuối';
    default:
      return loai;
  }
}

export function nhanKetQuaDocDuyet(ketQua: string): {
  label: string;
  color: 'green' | 'orange' | 'red';
} {
  switch (ketQua) {
    case 'DongY':
      return { label: 'Đồng ý cho bảo vệ', color: 'green' };
    case 'SuaGap':
      return { label: 'Yêu cầu sửa gấp', color: 'orange' };
    default:
      return { label: 'Không đồng ý', color: 'red' };
  }
}

/** Còn bao nhiêu ngày đến hạn đọc → màu: quá hạn đỏ, ≤1 ngày cam, còn lại xanh. */
export function mauHanDoc(conLaiNgay: number): 'red' | 'orange' | 'green' {
  if (conLaiNgay < 0) return 'red';
  if (conLaiNgay <= 1) return 'orange';
  return 'green';
}

export function nhanHanDoc(conLaiNgay: number): string {
  if (conLaiNgay < 0) return `Quá hạn ${-conLaiNgay} ngày`;
  if (conLaiNgay === 0) return 'Đến hạn hôm nay';
  return `Còn ${conLaiNgay} ngày`;
}

/** Mã hash rút gọn để hiển thị (đầy đủ ở tooltip/đối chiếu). */
export function hashRutGon(hash: string): string {
  return hash.length > 12 ? `${hash.slice(0, 8)}…${hash.slice(-4)}` : hash;
}
