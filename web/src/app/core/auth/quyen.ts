/**
 * Mã quyền – khớp `Upm.Domain.PhanQuyen.Quyen` ở backend. Danh sách quyền thực tế của người dùng
 * lấy từ `/api/v1/auth/me` (server là nguồn chuẩn; FE chỉ dùng để ẩn/hiện giao diện).
 */
export const Quyen = {
  TaiKhoanQuanLy: 'TaiKhoan.QuanLy',
  RuleSetQuanLy: 'RuleSet.QuanLy',
  DanhMucQuanLy: 'DanhMuc.QuanLy',
  AuditLogXem: 'AuditLog.Xem',
  DotQuanLy: 'Dot.QuanLy',
  XetDieuKienChay: 'XetDieuKien.Chay',
  QuyetDinhGhiNhan: 'QuyetDinh.GhiNhan',
  DeTaiDeXuat: 'DeTai.DeXuat',
  DeTaiDuyet: 'DeTai.Duyet',
  PhanCongQuanLy: 'PhanCong.QuanLy',
  HoiDongDeXuat: 'HoiDong.DeXuat',
  LichBaoVeXep: 'LichBaoVe.Xep',
  DiemChot: 'Diem.Chot',
  DiemSuaSauChot: 'Diem.SuaSauChot',
  SinhVienXem: 'SinhVien.Xem',
  BaoCaoXem: 'BaoCao.Xem',
  HoSoCuaToi: 'HoSo.CuaToi',
  ThuVienTiepNhan: 'ThuVien.TiepNhan',
} as const;

export type MaQuyen = (typeof Quyen)[keyof typeof Quyen];
