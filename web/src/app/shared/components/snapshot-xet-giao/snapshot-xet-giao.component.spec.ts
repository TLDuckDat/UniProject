import { registerLocaleData } from '@angular/common';
import localeVi from '@angular/common/locales/vi';
import { TestBed } from '@angular/core/testing';
import { provideNzIconsTesting } from 'ng-zorro-antd/icon/testing';

import type { XetGiaoSnapshotItem } from '../../../api/models/xet-giao-snapshot-item';
import { SnapshotXetGiaoComponent } from './snapshot-xet-giao.component';

function snapshot(overrides: Partial<XetGiaoSnapshotItem> = {}): XetGiaoSnapshotItem {
  return {
    id: 1,
    loaiXet: 'Giao',
    maRuleSet: 'UTC-2021',
    ruleVersion: 'UTC-2021',
    thoiDiemXet: '2026-11-09T03:00:00Z',
    nguoiChay: null,
    ketQua: 'KHONG_DU_DK',
    maNgoaiLe: 'A13',
    tcChuaTichLuy: 11,
    maxTCChuaTichLuy: 10,
    g1KyLuat: true,
    g2TrangThaiHocTap: true,
    g3TinChi: false,
    daDatTTTN: false,
    co: [],
    lyDo: 'Nợ 11 TC > 10.',
    chiTiets: [
      {
        maHP: 'KTH-TTTN',
        tenHP: 'Thực tập tốt nghiệp',
        loaiHP: 'TTTN',
        soTC: 6,
        loaiNo: 'TTTNCong',
        biLoaiTru: false,
        maNgoaiLe: 'A07',
        lyDo: 'Trễ tiến độ, chưa có điểm TTTN.',
      },
      {
        maHP: 'KTH105',
        tenHP: 'Cơ học đất',
        loaiHP: 'BatBuoc',
        soTC: 5,
        loaiNo: 'No',
        biLoaiTru: false,
        maNgoaiLe: null,
        lyDo: null,
      },
      {
        maHP: 'GDQP',
        tenHP: 'Giáo dục quốc phòng',
        loaiHP: 'GDQP',
        soTC: 8,
        loaiNo: 'No',
        biLoaiTru: true,
        maNgoaiLe: 'A05',
        lyDo: 'Không tính vào giới hạn.',
      },
    ],
    ...overrides,
  } as XetGiaoSnapshotItem;
}

describe('SnapshotXetGiaoComponent', () => {
  beforeAll(() => registerLocaleData(localeVi));

  async function render(snapshots: XetGiaoSnapshotItem[]): Promise<HTMLElement> {
    TestBed.configureTestingModule({
      imports: [SnapshotXetGiaoComponent],
      providers: [provideNzIconsTesting()],
    });
    const fixture = TestBed.createComponent(SnapshotXetGiaoComponent);
    fixture.componentRef.setInput('snapshots', snapshots);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('A07: hiển thị kết luận, mã ngoại lệ, từng học phần và tổng TC tính vào giới hạn', async () => {
    const el = await render([snapshot()]);
    const text = el.textContent ?? '';

    expect(text).toContain('Không đủ điều kiện');
    expect(text).toContain('A13');
    expect(text).toContain('11 / tối đa 10 TC');
    expect(text).toContain('TTTN bị cộng (trễ tiến độ)');
    expect(text).toContain('Không (loại trừ)');
    expect(text).toContain('Hệ thống');
    // Tổng chỉ cộng học phần không bị loại trừ: 6 + 5 (GDQP 8 TC loại trừ).
    expect(el.querySelector('nz-table tfoot')?.textContent).toContain('11');
    expect(el.querySelectorAll('nz-table tbody tr').length).toBe(3);
  });

  it('A06: cờ CHUA_QUA_TTTN được giải thích cho người đọc', async () => {
    const el = await render([
      snapshot({
        ketQua: 'DU_DK',
        maNgoaiLe: 'A06',
        tcChuaTichLuy: 0,
        co: ['CHUA_QUA_TTTN'],
        chiTiets: [],
      }),
    ]);
    const text = el.textContent ?? '';

    expect(text).toContain('Đủ điều kiện');
    expect(text).toContain('Chưa qua TTTN');
    expect(text).toContain('Không còn học phần nào chưa tích lũy.');
  });

  it('A19: mặc định hiển thị lần xét mới nhất, có ô chọn các lần xét cũ', async () => {
    const el = await render([
      snapshot(),
      snapshot({
        id: 2,
        loaiXet: 'XetLai',
        ketQua: 'DU_DK',
        maNgoaiLe: null,
        tcChuaTichLuy: 0,
        chiTiets: [],
      }),
    ]);

    expect(el.textContent).toContain('Đủ điều kiện');
    expect(el.textContent).toContain('Lần xét:');
    expect(el.textContent).toContain('Xét lại');
  });

  it('chưa có snapshot → thông báo chưa xét', async () => {
    const el = await render([]);
    expect(el.textContent).toContain('Chưa có kết quả xét giao.');
  });
});
