/** Nhãn tiếng Việt và màu cho đề tài & phân công (UC-2). Dùng chung cho các tab GV / SV / Trưởng BM. */

export type MauNhan = 'green' | 'red' | 'orange' | 'blue' | 'default' | 'purple';

export interface Nhan {
  label: string;
  color: MauNhan;
}

const TRANG_THAI_DE_TAI: Record<string, Nhan> = {
  Nhap: { label: 'Chờ GV xác nhận', color: 'orange' },
  ChoDuyet: { label: 'Chờ Bộ môn duyệt', color: 'orange' },
  DaDuyet: { label: 'Đã duyệt', color: 'green' },
  TuChoi: { label: 'Từ chối', color: 'red' },
};

export function nhanTrangThaiDeTai(trangThai: string): Nhan {
  return TRANG_THAI_DE_TAI[trangThai] ?? { label: trangThai, color: 'default' };
}

const LOAI_DE_TAI: Record<string, string> = {
  LyThuyet: 'Lý thuyết',
  CheTao: 'Chế tạo',
  HeThong: 'Hệ thống',
};

export function nhanLoaiDeTai(loai: string): string {
  return LOAI_DE_TAI[loai] ?? loai;
}

export const LOAI_DE_TAI_LUA_CHON: readonly { value: string; label: string }[] = [
  { value: 'LyThuyet', label: 'Lý thuyết (1 SV)' },
  { value: 'CheTao', label: 'Chế tạo (1–3 SV, cần Bộ môn duyệt)' },
  { value: 'HeThong', label: 'Hệ thống (1–3 SV, cần Bộ môn duyệt)' },
];

const NGUYEN_VONG: Record<string, Nhan> = {
  ChoDuyet: { label: 'Chờ GV duyệt', color: 'orange' },
  TrungTuyen: { label: 'Trúng tuyển', color: 'green' },
  Truot: { label: 'Trượt', color: 'red' },
};

export function nhanNguyenVong(trangThai: string): Nhan {
  return NGUYEN_VONG[trangThai] ?? { label: trangThai, color: 'default' };
}

/**
 * Mức tải GV (x/quota) theo đặc tả: Thấp &lt; 80% · Cao 80–99% · Đủ quota = 100% · Vượt quota &gt; 100%.
 * Giá trị server trả về (MucTaiGiangVien) – FE chỉ đổi sang màu/nhãn.
 */
const MUC_TAI: Record<string, Nhan> = {
  Thap: { label: 'Thấp', color: 'green' },
  Cao: { label: 'Cao', color: 'orange' },
  DuQuota: { label: 'Đủ quota', color: 'blue' },
  VuotQuota: { label: 'Vượt quota', color: 'red' },
};

export function nhanMucTai(muc: string): Nhan {
  return MUC_TAI[muc] ?? { label: muc, color: 'default' };
}

/** Tỉ lệ tải để vẽ thanh tiến độ (tối đa 100%). */
export function tyLeTai(dangCo: number, quota: number): number {
  if (quota <= 0) return dangCo > 0 ? 100 : 0;
  return Math.min(100, Math.round((dangCo * 100) / quota));
}

const DON_DIEU_CHINH: Record<string, string> = {
  DoiGVHD: 'Đổi GVHD',
  DoiDeTai: 'Đổi tên / nội dung đề tài',
};

export function nhanLoaiDon(loai: string): string {
  return DON_DIEU_CHINH[loai] ?? loai;
}

export function nhanVaiTro(vaiTro: string): string {
  return vaiTro === 'GVHD' ? 'GVHD' : vaiTro === 'GVDD' ? 'GVĐD' : 'Đồng HD';
}
