import type { MauTag } from '../../shared/components/snapshot-xet-giao/xet-giao-nhan';

export function nhanTrangThaiDot(trangThai: string): { label: string; color: MauTag } {
  switch (trangThai) {
    case 'Mo':
      return { label: 'Đang mở', color: 'green' };
    case 'DangChay':
      return { label: 'Đang diễn ra', color: 'blue' };
    case 'Dong':
      return { label: 'Đã đóng', color: 'default' };
    default:
      return { label: trangThai, color: 'default' };
  }
}

export function nhanTrangThaiDon(trangThai: string): { label: string; color: MauTag } {
  switch (trangThai) {
    case 'ChoXuLy':
      return { label: 'Chờ xử lý', color: 'orange' };
    case 'DaDuyet':
      return { label: 'Đã chấp nhận', color: 'green' };
    case 'TuChoi':
      return { label: 'Từ chối', color: 'red' };
    default:
      return { label: trangThai, color: 'default' };
  }
}
