// Enum nghiệp vụ theo docs/thiet-ke.md (ERD-01 … ERD-07). Lưu xuống DB dạng chuỗi (tên thành viên),
// vì vậy KHÔNG đổi tên thành viên khi đã có dữ liệu.
namespace Upm.Domain.Enums;

// ---------- ERD-01 · Tổ chức & nhân sự ----------
public enum KhoiNganh { KyThuat, KinhTe }

public enum LoaiGiangVien { CoHuu, ThinhGiang, NgoaiTruong }

public enum TrangThaiHocTap { DangHoc, NghiTamThoi, BuocThoiHoc, DaThoiHoc, DaTotNghiep }

public enum HeDaoTao { CuNhan, KySuChuyenSau }

/// <summary>Dùng chung cho ChuongTrinhDaoTao.ChuongTrinh và ThamSoDaoVan.ChuongTrinh.</summary>
public enum LoaiChuongTrinh { DaiTra, CLC, TienTien }

public enum LoaiTaiKhoan { NoiBo, SV, GV, HeThongNgoai }

// ---------- ERD-02 · Cấu hình & dữ liệu đồng bộ ----------
public enum LoaiHocPhan { GDQP, GDTC, TTTN, DATN, Thuong }

/// <summary>I = hoãn thi, X = thiếu dữ liệu, R = miễn/công nhận TC (đã tích lũy, không vào GPA).</summary>
public enum TrangThaiDiem { Dat, I, X, R, DangHoc, Rot }

// ---------- ERD-03 · Đợt & xét điều kiện giao ----------
public enum TrangThaiDot { Mo, DangChay, Dong }

public enum LoaiXet { Giao, BaoVe, XetLai }

public enum LoaiNo { No, DangHoc, DiemI, DiemX, TTTNCong }

// ---------- ERD-04 · Đề tài, phân công, tiến độ ----------
public enum MaMocTienDo { M1, M2, M3 }

public enum LoaiDeTai { LyThuyet, CheTao, HeThong }

public enum NguonDeXuat { GV, SV }

public enum TrangThaiDuyetDeTai { Nhap, ChoDuyet, DaDuyet, TuChoi }

public enum KetQuaTienDo { Dat, CanhBao, KhongNop }

public enum TrangThaiNguyenVong { ChoDuyet, TrungTuyen, Truot }

public enum VaiTroPhanCong { GVHD, GVDD, DongHD }

// ---------- ERD-05 · Quyển, đọc duyệt, đạo văn ----------
public enum LoaiBanQuyen { NopQuyen, BanSuaGap, BanSuaSauBV, BanCuoi }

public enum KetQuaDocDuyetLoai { DongY, SuaGap, KhongDongY }

// ---------- ERD-06 · Hội đồng, bảo vệ, điểm ----------
public enum HinhThucBaoVe { TrucTiep, TrucTuyen }

public enum TrangThaiHoiDong { Nhap, DaDuyet, DaMoPhien, DaChot }

public enum ChucDanhHoiDong { ChuTich, ThuKy, UyVien }

public enum TrangThaiLichBaoVe { DuKien, DaBaoVe, VangCoLyDo, VangKhongLyDo, BaoVeBu }

// ---------- ERD-07 · Quyết định, đơn từ, log ----------
public enum LoaiQuyetDinh { GiaoDATN, DieuChinh, ThanhLapHD, SuaDiem, Khac }

public enum LoaiDon { DoiDeTai, GiaHan, HoanBaoVe, VangBaoVe, XinDung, KhieuNai, PhucKhao }

public enum TrangThaiDonTu { ChoXuLy, DaDuyet, TuChoi }

public enum HanhDongAudit { Insert, Update, Delete, Transition }

public enum LoaiNhac { MocTienDo, HanDocDuyet, HanSuaGap, HanHocKyLienKe, HanBanCuoi }
