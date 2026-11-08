import { Routes } from '@angular/router';

import { taoMenu } from './menu';

describe('taoMenu', () => {
  const routes: Routes = [
    {
      path: 'quan-tri',
      data: { menu: { nhan: 'Quản trị', icon: 'setting', thuTu: 80 }, quyen: ['TaiKhoan.QuanLy'] },
    },
    { path: '', data: { menu: { nhan: 'Trang chủ', icon: 'home', thuTu: 0 } } },
    {
      path: 'dot-datn',
      data: { menu: { nhan: 'Đợt', icon: 'calendar', thuTu: 10 }, quyen: ['Dot.QuanLy'] },
    },
    { path: '403' }, // không có data.menu → không hiện
  ];

  it('lọc theo quyền và sắp theo thứ tự', () => {
    const menu = taoMenu(routes, (...q) => q.includes('Dot.QuanLy'));

    expect(menu.map((m) => m.duongDan)).toEqual(['/', '/dot-datn']);
  });

  it('không quyền nào vẫn thấy mục không yêu cầu quyền', () => {
    expect(taoMenu(routes, () => false).map((m) => m.nhan)).toEqual(['Trang chủ']);
  });
});
