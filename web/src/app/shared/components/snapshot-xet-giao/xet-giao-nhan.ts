/**
 * Nhãn tiếng Việt cho kết quả xét giao ĐATN (đặc tả mục 6, ngoại lệ A01–A21) – dùng chung cho
 * màn hình Phòng ĐT và trang "Kết quả xét giao ĐATN" của sinh viên.
 */
import type { LoaiNo } from '../../../api/models/loai-no';

export type MauTag = 'green' | 'red' | 'orange' | 'blue' | 'default';

export interface NhanKetQua {
  label: string;
  color: MauTag;
}

const KET_QUA: Record<string, NhanKetQua> = {
  DU_DK: { label: 'Đủ điều kiện', color: 'green' },
  KHONG_DU_DK: { label: 'Không đủ điều kiện', color: 'red' },
  KHONG_DU_DK_CHUYEN_THAY_THE: { label: 'Chuyển học phần thay thế', color: 'orange' },
};

export function nhanKetQua(ketQua: string | null | undefined): NhanKetQua {
  if (!ketQua) {
    return { label: 'Chưa xét', color: 'default' };
  }
  return KET_QUA[ketQua] ?? { label: ketQua, color: 'blue' };
}

export const KET_QUA_LOC: readonly { value: string; label: string }[] = [
  { value: 'CHUA_XET', label: 'Chưa xét' },
  { value: 'DU_DK', label: 'Đủ điều kiện' },
  { value: 'KHONG_DU_DK', label: 'Không đủ điều kiện' },
  { value: 'KHONG_DU_DK_CHUYEN_THAY_THE', label: 'Chuyển học phần thay thế' },
];

const LOAI_NO: Record<LoaiNo, string> = {
  No: 'Chưa đạt / chưa học',
  DangHoc: 'Đang học, chưa có điểm',
  DiemI: 'Điểm I (hoãn thi)',
  DiemX: 'Điểm X (thiếu dữ liệu)',
  TTTNCong: 'TTTN bị cộng (trễ tiến độ)',
};

export function nhanLoaiNo(loai: LoaiNo): string {
  return LOAI_NO[loai] ?? loai;
}

const CO: Record<string, { label: string; moTa: string }> = {
  NO_TIN_CHI: {
    label: 'Còn nợ tín chỉ',
    moTa: 'Được nhận ĐATN nhưng phải trả hết nợ trước khi xét bảo vệ.',
  },
  CHUA_QUA_TTTN: {
    label: 'Chưa qua TTTN',
    moTa: 'Bình thường với SV đúng tiến độ (đi TTTN song song); phải đạt TTTN trước khi bảo vệ.',
  },
};

export function nhanCo(co: string): { label: string; moTa: string } {
  return CO[co] ?? { label: co, moTa: '' };
}

/** Mô tả ngắn các mã ngoại lệ xét giao (đặc tả mục 10, nhóm A). */
const NGOAI_LE: Record<string, string> = {
  A01: 'Nợ (đã loại trừ) ≤ 10 TC: được giao, gắn cờ còn nợ tín chỉ.',
  A02: 'Nợ quá 10 TC: không đủ điều kiện.',
  A03: 'Nợ đúng 10 TC: vẫn được giao (điều kiện ≤).',
  A04: 'Môn đang học chưa có điểm: tính là chưa tích lũy.',
  A05: 'GDQP, GDTC, ĐATN: không tính vào giới hạn 10 TC.',
  A06: 'Đúng tiến độ, chưa học TTTN: TTTN không tính, đi TTTN song song.',
  A07: 'Trễ tiến độ, chưa có điểm TTTN: TC TTTN bị cộng vào giới hạn.',
  A08: 'Điểm I / X: chưa tích lũy, xét lại khi có điểm.',
  A09: 'Học phần R (miễn, công nhận): tính là đã tích lũy.',
  A10: 'Đang bị đình chỉ học tập / truy cứu TNHS: không đủ điều kiện.',
  A11: 'Đang nghỉ học tạm thời / bảo lưu: không đưa vào đợt.',
  A12: 'Trạng thái học tập không hợp lệ hoặc quá thời gian học tối đa.',
  A13: 'Khối Kỹ thuật không có học phần thay thế: trả nợ để nhận ĐATN kỳ sau.',
  A14: 'Khối Kinh tế / Cử nhân: chuyển đăng ký học phần chuyên môn thay thế.',
  A15: 'Áp RuleSet theo khóa sinh viên.',
  A16: 'K66: tạm áp UTC-2021, chờ xác nhận quy chế.',
  A18: 'Học 2 chương trình: 2 hồ sơ ĐATN độc lập.',
  A19: 'Điểm thay đổi sau khi xét: đã xét lại, giữ kết quả cũ.',
  A20: 'Khiếu nại kết quả xét.',
  A21: 'Làm lại ĐATN từ đợt trước: tính là học lại.',
};

export function moTaNgoaiLe(ma: string | null | undefined): string {
  return ma ? (NGOAI_LE[ma] ?? '') : '';
}

export function nhanLoaiXet(loai: string): string {
  return loai === 'XetLai' ? 'Xét lại' : loai === 'Giao' ? 'Xét giao' : loai;
}

/** Trạng thái job Hangfire → nhãn tiếng Việt. */
export function nhanTrangThaiJob(trangThai: string): string {
  switch (trangThai) {
    case 'Enqueued':
    case 'Scheduled':
    case 'Awaiting':
      return 'Đang chờ chạy';
    case 'Processing':
      return 'Đang xét';
    case 'Succeeded':
      return 'Đã xong';
    case 'Failed':
      return 'Lỗi';
    case 'Deleted':
      return 'Đã hủy';
    default:
      return trangThai;
  }
}

export function jobDangChay(trangThai: string | null | undefined): boolean {
  return (
    trangThai === 'Enqueued' ||
    trangThai === 'Processing' ||
    trangThai === 'Scheduled' ||
    trangThai === 'Awaiting'
  );
}
