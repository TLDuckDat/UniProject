import { Route, Routes } from '@angular/router';

/** Khai báo mục menu ngay trên route: `data: { menu: { nhan: 'Đợt ĐATN', icon: 'calendar', thuTu: 10 }, quyen: [...] }`. */
export interface MenuData {
  nhan: string;
  icon: string;
  thuTu: number;
}

export interface MenuItem extends MenuData {
  duongDan: string;
}

/**
 * Sinh menu từ cấu hình route con của layout: lấy route có `data.menu`, lọc theo `data.quyen`
 * (có MỘT quyền là đủ; không khai báo = ai đăng nhập cũng thấy), sắp theo `thuTu`.
 */
export function taoMenu(
  routes: Routes | undefined,
  coQuyen: (...quyen: string[]) => boolean,
): MenuItem[] {
  return (routes ?? [])
    .filter((r): r is Route & { data: { menu: MenuData } } => !!r.data?.['menu'])
    .filter((r) => {
      const quyen = r.data['quyen'] as string[] | undefined;
      return !quyen?.length || coQuyen(...quyen);
    })
    .map((r) => ({ ...r.data.menu, duongDan: '/' + (r.path ?? '') }))
    .sort((a, b) => a.thuTu - b.thuTu);
}
