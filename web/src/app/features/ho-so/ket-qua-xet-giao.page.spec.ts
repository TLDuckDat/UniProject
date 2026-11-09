import { registerLocaleData } from '@angular/common';
import localeVi from '@angular/common/locales/vi';
import { TestBed } from '@angular/core/testing';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';
import { of } from 'rxjs';

import type { HoSoCuaToiXetGiaoItem } from '../../api/models/ho-so-cua-toi-xet-giao-item';
import { HoSoCuaToiService } from '../../api/services/ho-so-cua-toi.service';
import { KetQuaXetGiaoPage } from './ket-qua-xet-giao.page';

const hoSo: HoSoCuaToiXetGiaoItem = {
  svTrongDotId: 7,
  dotId: 1,
  maDot: 'D2026-1',
  tenDot: 'Đợt ĐATN HK1 2026-2027',
  maCTDT: '7580205-HN',
  tenCTDT: 'Kỹ thuật xây dựng',
  trangThai: 'CHO_XET',
  daCoQuyetDinhGiao: false,
  coTheKhieuNai: true,
  ketQuaMoiNhat: {
    id: 1,
    loaiXet: 'Giao',
    maRuleSet: 'UTC-2021',
    ruleVersion: 'UTC-2021',
    thoiDiemXet: '2026-11-09T03:00:00Z',
    ketQua: 'DU_DK',
    maNgoaiLe: 'A01',
    tcChuaTichLuy: 6,
    maxTCChuaTichLuy: 10,
    co: ['NO_TIN_CHI'],
    chiTiets: [],
  },
  khieuNai: [
    {
      id: 3,
      svTrongDotId: 7,
      maSV: '65HN001',
      hoTen: 'Nguyễn Văn An',
      noiDung: 'Đề nghị xét lại.',
      ngayGui: '2026-11-09T04:00:00Z',
      trangThai: 'TuChoi',
      ketQuaXuLy: 'Điểm chưa cập nhật.',
    },
  ],
};

describe('KetQuaXetGiaoPage', () => {
  beforeAll(() => registerLocaleData(localeVi));

  function setup(items: HoSoCuaToiXetGiaoItem[]) {
    const api = {
      hoSoCuaToiXetGiao: vi.fn(() => of(items)),
      hoSoCuaToiKhieuNaiXetGiao: vi.fn(() => of({ donTuId: 9 })),
    };
    TestBed.configureTestingModule({
      imports: [KetQuaXetGiaoPage],
      providers: [provideNzIconsTesting(), { provide: HoSoCuaToiService, useValue: api }],
    });
    return { fixture: TestBed.createComponent(KetQuaXetGiaoPage), api };
  }

  it('hiển thị kết quả, cờ nợ TC, phản hồi khiếu nại và nút khiếu nại', async () => {
    const { fixture } = setup([hoSo]);
    await fixture.whenStable();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(text).toContain('Đợt ĐATN HK1 2026-2027');
    expect(text).toContain('Đủ điều kiện');
    expect(text).toContain('Còn nợ tín chỉ');
    expect(text).toContain('Điểm chưa cập nhật.');
    expect(text).toContain('Khiếu nại kết quả');
  });

  it('không cho khiếu nại khi đã có quyết định giao', async () => {
    const { fixture } = setup([{ ...hoSo, daCoQuyetDinhGiao: true, coTheKhieuNai: false }]);
    await fixture.whenStable();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(text).toContain('Đã có quyết định giao ĐATN.');
    expect(text).not.toContain('Khiếu nại kết quả');
  });

  it('chưa có hồ sơ → thông báo trống', async () => {
    const { fixture } = setup([]);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'Bạn chưa có hồ sơ trong đợt ĐATN nào.',
    );
  });
});
