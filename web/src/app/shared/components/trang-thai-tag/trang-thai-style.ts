export interface TrangThaiStyle {
  label: string;
  color: 'blue' | 'orange' | 'red' | 'green';
}

const styles: Record<string, TrangThaiStyle> = {
  CHO_XET: { label: 'Chờ xét', color: 'orange' },
  KHONG_DU_DK: { label: 'Không đủ điều kiện', color: 'red' },
  KHONG_DU_DK_CHUYEN_THAY_THE: { label: 'Học phần thay thế', color: 'red' },
  DA_GIAO: { label: 'Đã giao', color: 'blue' },
  DANG_THUC_HIEN: { label: 'Đang thực hiện', color: 'blue' },
  TAM_DUNG: { label: 'Tạm dừng', color: 'orange' },
  RUT: { label: 'Đã rút', color: 'red' },
  DA_NOP_QUYEN: { label: 'Đã nộp quyển', color: 'blue' },
  YEU_CAU_CHINH_SUA: { label: 'Yêu cầu chỉnh sửa', color: 'orange' },
  DUOC_BAO_VE: { label: 'Được bảo vệ', color: 'blue' },
  HOAN_BAO_VE: { label: 'Hoãn bảo vệ', color: 'orange' },
  PHAI_LAM_LAI: { label: 'Phải làm lại', color: 'red' },
  DA_BAO_VE: { label: 'Đã bảo vệ', color: 'blue' },
  DAT: { label: 'Đạt', color: 'green' },
  KHONG_DAT: { label: 'Không đạt', color: 'red' },
  CHO_BAN_CUOI: { label: 'Chờ bản cuối', color: 'orange' },
  HOAN_TAT: { label: 'Hoàn tất', color: 'green' },
};

export function trangThaiStyle(status: string): TrangThaiStyle {
  return styles[status] ?? { label: status.replaceAll('_', ' ').toLowerCase(), color: 'blue' };
}
