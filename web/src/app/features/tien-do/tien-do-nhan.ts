/** Nhãn và màu cho tiến độ 3 mốc (UC-3a). Dùng chung cho tab SV / GVHD / Bộ môn. */

export type MauTag = 'green' | 'red' | 'orange' | 'blue' | 'default';

export interface NhanTienDo {
  label: string;
  color: MauTag;
}

/** Trạng thái mốc: Dat / CanhBao (C02) / KhongNop (C01 – quá hạn chưa nộp) / null (chưa đến hạn, chưa nộp). */
export function nhanTrangThaiMoc(trangThai: string | null | undefined): NhanTienDo {
  switch (trangThai) {
    case 'Dat':
      return { label: 'Đạt', color: 'green' };
    case 'CanhBao':
      return { label: 'Dưới yêu cầu (C02)', color: 'orange' };
    case 'KhongNop':
      return { label: 'Quá hạn chưa nộp (C01)', color: 'red' };
    default:
      return { label: 'Chưa đến hạn', color: 'default' };
  }
}

const TEN_MOC: Record<string, string> = {
  M1: 'Mốc 1 – Đề cương / Chương 1 (tuần 3–4)',
  M2: 'Mốc 2 – Tiến độ 60–70% (tuần 8–9)',
  M3: 'Mốc 3 – Duyệt 100% và nộp quyển (tuần 12–13)',
};

export function tenMoc(maMoc: string): string {
  return TEN_MOC[maMoc] ?? maMoc;
}

export function nhanLoaiDonTienDo(loai: string): string {
  return loai === 'GiaHan' ? 'Gia hạn mốc' : loai === 'XinDung' ? 'Xin dừng ĐATN' : loai;
}

export function nhanTrangThaiDon(trangThai: string): NhanTienDo {
  switch (trangThai) {
    case 'ChoXuLy':
      return { label: 'Chờ duyệt', color: 'orange' };
    case 'DaDuyet':
      return { label: 'Đã duyệt', color: 'green' };
    case 'TuChoi':
      return { label: 'Từ chối', color: 'red' };
    default:
      return { label: trangThai, color: 'default' };
  }
}

/** Ngày hạn ở dạng yyyy-MM-dd (từ server) → dd/MM/yyyy. */
export function ngayVn(iso: string | null | undefined): string {
  if (!iso) return '—';
  const [y, m, d] = iso.slice(0, 10).split('-');
  return `${d}/${m}/${y}`;
}
