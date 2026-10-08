# Dữ liệu seed mẫu

Toàn bộ dữ liệu trong thư mục này là **hư cấu** (họ tên ghép ngẫu nhiên, MSSV dạng `xx99071xxx`, tài khoản giảng viên `gv01`, `tg01`...).

Khi khởi động với `SeedSampleData=true`, mỗi file được tìm ở `Data/Seed/` trước (dữ liệu thật, **không commit**, đã có trong `.gitignore`),
không có thì dùng file cùng tên ở đây.

| File | Nội dung |
|---|---|
| `lecturers.json` | Giảng viên cơ hữu (mã GV001... theo thứ tự, GV001 là Trưởng Bộ môn) và thỉnh giảng (`visiting: true`, mã TG001...) |
| `students.json` | Sinh viên theo lớp: `{ "CQ.65.CNTT": [["MSSV", "Họ tên"], ...] }` |
| `k65-do-an-co-so.json` | Đợt Đồ án cơ sở K65: `assignments` (MSSV, tên đề tài); sinh viên cùng đề tài được gom thành một nhóm |
| `k65-do-an-mon-hoc.json` | Đồ án môn học: nhóm cố định, các nhóm Lập trình trực quan, GV phụ trách và hội đồng môn Công nghệ Java |
| `datn-2026-03.json` | Đợt Đồ án tốt nghiệp: sinh viên, đề tài (phiếu nhiệm vụ), GVHD, GV đọc duyệt, hội đồng; `report` là tên file PDF trong thư mục `datn-2026-03/` cạnh file JSON |
