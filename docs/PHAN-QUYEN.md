# Ma trận phân quyền – UPM (để BA rà soát)

Nguồn trong code: `src/Upm.Domain/PhanQuyen/Quyen.cs` (`Quyen`, `BangQuyen`). Unit test
`MaTranPhanQuyenTests` so khớp bảng dưới với code – **đổi code mà quên sửa tài liệu (hoặc ngược lại) thì test đỏ**.

Ký hiệu: **✔** = có quyền · **✔⚠** = đang cấp nhưng là SUY ĐOÁN, cần BA xác nhận · **⚠** = đang KHÔNG cấp, cần BA xác nhận
có nên cấp không · ô trống = không có quyền.

Quyền chức năng chỉ trả lời "được làm loại thao tác này không". **Dữ liệu nào** thì do phạm vi vai trò quyết định
(`TaiKhoanVaiTro`: cơ sở → khoa → bộ môn, NULL = rộng hơn) và vai trò trên từng SV (mục 3).

## 1. Ma trận Vai trò × Quyền

| Quyền | `QuanTri` | `PhongDaoTao` | `LanhDaoKhoa` | `TruongBoMon` | `GiaoVu` | `GiangVien` | `SinhVien` | `ThuVien` | Phạm vi áp dụng | Use case |
|---|---|---|---|---|---|---|---|---|---|---|
| `TaiKhoan.QuanLy` | ✔ | | | | | | | | Toàn trường | UC-1 Quản lý tài khoản, phân quyền |
| `RuleSet.QuanLy` | ✔ | | | | | | | | Toàn trường | UC-1 Quản lý RuleSet theo khóa |
| `DanhMuc.QuanLy` | ✔ | | | | | | | | Toàn trường | UC-1 (danh mục cơ sở/khoa/bộ môn – mục Actor) |
| `AuditLog.Xem` | ✔ | ✔⚠ | | | | | | | QuanTri: toàn trường · PĐT: cơ sở | UC-5 Sửa điểm sau chốt (bắt buộc log) |
| `Dot.QuanLy` | | ✔ | | | | | | | Cơ sở | UC-1 Mở đợt, cấu hình mốc & tham số |
| `XetDieuKien.Chay` | | ✔ | | | | | | | Cơ sở | UC-1 Xét điều kiện giao · UC-4 Xét bảo vệ K1–K6 |
| `QuyetDinh.GhiNhan` | | ✔ | | | | | | | Cơ sở | UC-1 QĐ giao · UC-4 QĐ thành lập HĐ · UC-5 QĐ sửa điểm |
| `DeTai.DeXuat` | | | | | | ✔ | ✔ | | GV: bộ môn của mình · SV: bản thân | UC-2 Đề xuất đề tài / SV tự đề xuất |
| `DeTai.Duyet` | | | ✔⚠ | ✔ | | | | | TBM: bộ môn · LĐK: khoa | UC-2 Duyệt đề tài |
| `PhanCong.QuanLy` | | | ⚠ | ✔ | | | | | Bộ môn | UC-2 Phân công GVHD/GVĐD, duyệt vượt quota, đổi GVHD |
| `HoiDong.DeXuat` | | | ✔ | | | | | | Khoa | UC-4 Đề xuất Hội đồng |
| `LichBaoVe.Xep` | | | | | ✔ | | | | Khoa | UC-4 Xếp lịch bảo vệ (phòng, ca) |
| `Diem.Chot` | | ✔ | | | | | | | Cơ sở | UC-5 Chốt điểm ĐATN |
| `Diem.SuaSauChot` | | ✔ | | | | | | | Cơ sở (bắt buộc số QĐ + log) | UC-5 Sửa điểm sau khi chốt |
| `SinhVien.Xem` | ✔⚠ | ✔ | ✔ | ✔⚠ | ✔⚠ | ✔⚠ | | | Theo phạm vi vai trò (cơ sở / khoa / bộ môn) | UC-1…UC-4 tra cứu SV |
| `BaoCao.Xem` | | ✔ | ✔ | | ⚠ | | | | PĐT: cơ sở · LĐK: khoa | Actor: Lãnh đạo Khoa xem báo cáo |
| `HoSo.CuaToi` | | | | | | | ✔ | | Bản thân | UC-1…UC-5 phía SV (xem kết quả, nộp, gửi đơn) |
| `ThuVien.TiepNhan` | | | | | | | | ✔ | Cơ sở | UC-5 Tiếp nhận, lưu trữ bản cuối |

## 2. Các ô ⚠ – câu hỏi cho BA

1. **Bảng vai trò → quyền**: Giáo vụ có xem báo cáo (`BaoCao.Xem`) không? Lãnh đạo Khoa có được phân công GVHD
   (`PhanCong.QuanLy`) hay chỉ duyệt đề tài (`DeTai.Duyet` – đang cấp tạm)? GV thường có xem danh sách SV cả khoa
   (`SinhVien.Xem` – đang cấp tạm theo phạm vi khoa) hay chỉ SV mình hướng dẫn/đọc duyệt?
2. **Đăng nhập**: trường có SSO (CAS/LDAP/Microsoft 365) cần tích hợp không? Chính sách mật khẩu, khóa tài khoản?
3. **Trưởng Bộ môn** có được xem toàn bộ SV của khoa (để phân công) hay chỉ SV đã được gán vào bộ môn?
   (Hiện phạm vi bộ môn không thấy SV qua tra cứu – chỉ thấy qua phân công.)

Ngoài ra: Quản trị có cần xem dữ liệu SV (`SinhVien.Xem` ✔⚠) hay chỉ quản trị hệ thống? Phòng ĐT có xem toàn bộ
AuditLog của cơ sở (`AuditLog.Xem` ✔⚠)?

## 3. Quyền theo vai trò trên từng SV (không nằm trong ma trận)

Kiểm qua `IQuyenTrenSV` trên từng hồ sơ `SVTrongDot`, KHÔNG phụ thuộc vai trò hệ thống:

| Kiểm tra | Nguồn dữ liệu | Dùng cho |
|---|---|---|
| `LaGVHD` | `PhanCong` (VaiTro = GVHD, còn hiệu lực) | Chấm tiến độ, điểm GVHD, đồng ý/không đồng ý bảo vệ (UC-3) |
| `LaGVDD` | `PhanCong` (VaiTro = GVDD, còn hiệu lực) | Đọc duyệt, điểm GVĐD, yêu cầu sửa gấp (UC-3) |
| `LaThanhVienHoiDong` | `LichBaoVe` + `ThanhVienHoiDong` | Chấm phiếu điểm, biên bản, mở/chốt phiên (UC-4) |
| `LaChinhSV` | `SVTrongDot.SinhVienId` | SV thao tác trên hồ sơ của chính mình |
