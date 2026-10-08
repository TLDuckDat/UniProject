# DDCom — Hệ thống quản lý đồ án

Ứng dụng web quản lý toàn bộ vòng đời đồ án của Bộ môn Công nghệ Thông tin, theo cơ cấu và quy trình của Phân hiệu Trường Đại học Giao thông Vận tải tại TP.HCM (UTC2). Hệ thống hỗ trợ bốn loại đồ án: **Đồ án cơ sở**, **Đồ án chuyên ngành**, **Đồ án môn học** và **Đồ án tốt nghiệp**.

Chức năng chính: mở đợt đồ án; đề xuất và duyệt đề tài (do giảng viên hoặc sinh viên đề xuất); đăng ký nhóm theo nguyện vọng; đổi đề tài hoặc đổi nhóm sau khi đơn đã duyệt; thực hiện đồ án (báo cáo tiến độ, nộp tài liệu, yêu cầu chỉnh sửa); phản biện (đọc duyệt); lập hội đồng và lịch bảo vệ; chấm điểm theo vai trò; công bố kết quả; gửi thông báo.

| Thành phần | Công nghệ |
|---|---|
| Backend | .NET 10, ASP.NET Core Web API, Entity Framework Core 10, SQL Server |
| Kiến trúc | Modular Monolith: mỗi nghiệp vụ là một module độc lập trong cùng một project |
| Bảo mật | JWT (access token và refresh token xoay vòng), phân quyền theo vai trò và theo quyền sở hữu dữ liệu |
| Frontend | HTML, CSS, JavaScript thuần (ES modules), không cần build |

---

## Mục lục

1. [Chạy nhanh](#1-chạy-nhanh)
2. [Các loại đồ án](#2-các-loại-đồ-án)
3. [Vai trò và phân quyền](#3-vai-trò-và-phân-quyền)
4. [Quy trình nghiệp vụ](#4-quy-trình-nghiệp-vụ)
5. [Giao diện web](#5-giao-diện-web)
6. [Cấu trúc dự án](#6-cấu-trúc-dự-án)
7. [Design pattern sử dụng](#7-design-pattern-sử-dụng)
8. [Cài đặt và cấu hình chi tiết](#8-cài-đặt-và-cấu-hình-chi-tiết)
9. [Dữ liệu mẫu và tài khoản demo](#9-dữ-liệu-mẫu-và-tài-khoản-demo)
10. [Bảo vệ dữ liệu khi đưa lên Git](#10-bảo-vệ-dữ-liệu-khi-đưa-lên-git)
11. [Danh sách API](#11-danh-sách-api)
12. [Quy ước phản hồi và mã lỗi](#12-quy-ước-phản-hồi-và-mã-lỗi)

---

## 1. Chạy nhanh

Yêu cầu: [.NET SDK 10](https://dotnet.microsoft.com/download) và SQL Server (bản đầy đủ, Express hoặc LocalDB).

```bash
# 1. Tạo file cấu hình rồi sửa chuỗi kết nối và Jwt__Key (tối thiểu 32 ký tự)
cp backend/UniProject.API/.env.example backend/UniProject.API/.env

# 2. Chạy backend: tự tạo database, chạy migration, seed dữ liệu mẫu (http://localhost:5000)
cd backend/UniProject.API
dotnet run

# 3. Chạy frontend ở terminal khác (http://localhost:5500)
cd frontend
python -m http.server 5500
```

Mở `http://localhost:5500`, đăng nhập bằng `admin` / `Admin@123456`, hoặc các tài khoản ở [mục 9](#9-dữ-liệu-mẫu-và-tài-khoản-demo). Repo đã kèm dữ liệu mẫu hư cấu, nên clone về là có đủ đợt đồ án, đề tài, nhóm, điểm để dùng thử.

---

## 2. Các loại đồ án

Mỗi **đợt đồ án** và mỗi **đề tài** thuộc đúng một loại. Luật của từng loại nằm trong một policy riêng (`Modules/ProjectPeriod/ProjectTypes`). Các service nghiệp vụ chỉ hỏi policy, không viết `if/else` theo loại.

| Loại | Mã | Dùng cho |
|---|---|---|
| Đồ án cơ sở | `DO_AN_CO_SO` | Đồ án cơ sở ngành, làm theo nhóm nhỏ |
| Đồ án chuyên ngành | `DO_AN_CHUYEN_NGANH` | Đồ án chuyên ngành, có giảng viên phản biện |
| Đồ án môn học | `DO_AN_MON_HOC` | Đồ án của một học phần, vd Công nghệ Java, Lập trình trực quan |
| Đồ án tốt nghiệp | `DO_AN_TOT_NGHIEP` | Mỗi sinh viên một đề tài, có giảng viên đọc duyệt (phản biện) |

Quy chế đào tạo của trường (QĐ 2006/QĐ-ĐHGTVT ngày 05/11/2021) quy định điểm đồ án tốt nghiệp bằng 0,4 × điểm quá trình (hướng dẫn, đọc duyệt) + 0,6 × điểm bảo vệ. Trọng số mặc định của Đồ án tốt nghiệp và Đồ án chuyên ngành (20 / 20 / 60) đúng bằng công thức này. Quy chế **không** quy định riêng cho các loại còn lại về quy mô nhóm, có phản biện hay không, mức điểm đạt, chấm riêng hay chấm chung. Vì vậy mọi tham số đều **cấu hình được, không cần sửa mã**.

Bảng dưới là **giá trị mặc định**, cấu hình trong `appsettings.json`, mục `ProjectTypes`:

| Tham số (khóa cấu hình) | Đồ án cơ sở | Đồ án chuyên ngành | Đồ án môn học | Đồ án tốt nghiệp |
|---|---|---|---|---|
| Quy mô nhóm tối đa (`MaxMembers`) | **2** sinh viên | **3** sinh viên | **4** sinh viên | **1** sinh viên |
| Giảng viên phản biện (`HasReviewer`) | Không | **Có** | Không | **Có** (đọc duyệt) |
| Trọng số % hướng dẫn / phản biện / hội đồng (`AdvisorWeightPercent`, `ReviewerWeightPercent`, `CommitteeWeightPercent`) | 40 / 0 / 60 | 20 / 20 / 60 | 40 / 0 / 60 | 20 / 20 / 60 |
| Điểm tổng kết tối thiểu để đạt (`PassingScore`) | 5 | 5 | 5 | 5 |
| Mức tối thiểu mỗi cột điểm bắt buộc (`MinComponentScore`, `0` = tắt) | 4 | 4 | 4 | 4 |
| Chế độ chấm (`GradingMode`) | `CHAM_RIENG` | `CHAM_RIENG` | `CHAM_RIENG` | `CHAM_RIENG` |
| Điều kiện tham gia (cố định trong mã) | Không | Sinh viên phải **đã đạt** một Đồ án cơ sở (kết quả đã công bố) | Không | Không (Phòng Đào tạo xét) |

**Đổi cấu hình:** sửa `appsettings.json`, hoặc đặt biến môi trường (hay dòng trong `.env`) theo dạng `ProjectTypes__<LOAI>__<Khóa>`, ví dụ:

```bash
ProjectTypes__DO_AN_CO_SO__MaxMembers=3
ProjectTypes__DO_AN_CO_SO__GradingMode=CHAM_CHUNG
ProjectTypes__DO_AN_MON_HOC__MaxMembers=5
ProjectTypes__DO_AN_CHUYEN_NGANH__MinComponentScore=0
```

Khóa nào không khai báo thì giữ giá trị mặc định. Cấu hình được kiểm tra khi khởi động; sai thì ứng dụng **dừng** và in danh sách lỗi. Các điều kiện kiểm tra:
- `MaxMembers` từ 1 đến 10, khớp ràng buộc `CK_DETAI_SoLuongSinhVien` của bảng đề tài.
- Ba trọng số không âm và tổng bằng **100**. `HasReviewer = false` thì `ReviewerWeightPercent` phải bằng 0; `HasReviewer = true` thì phải lớn hơn 0.
- `PassingScore` trong khoảng (0, 10]; `MinComponentScore` từ 0 đến 10; `GradingMode` là `CHAM_RIENG` hoặc `CHAM_CHUNG`.

Thay đổi có hiệu lực sau khi khởi động lại ứng dụng. Nên đổi trước khi mở đợt, vì điểm tổng kết được tính lại theo cấu hình đang chạy mỗi lần nhập điểm.

Luật chung cho mọi loại:
- **Xếp loại** (cố định): từ 9 là Xuất sắc, từ 8 là Giỏi, từ 6.5 là Khá, từ 5 là Trung bình, còn lại là Yếu.
- **Không đạt** nếu điểm tổng kết dưới `PassingScore`, hoặc có cột điểm bắt buộc (cột có trọng số lớn hơn 0) dưới `MinComponentScore`.
- **Chưa đủ cột điểm bắt buộc** thì kết quả là `CHO_KET_QUA`: chưa có tổng kết và chưa công bố được.
- **Điểm từng cột** là trung bình các phiếu chấm cùng vai trò. Ví dụ cột hội đồng là trung bình điểm của các thành viên hội đồng.
- **Chấm chung** (`CHAM_CHUNG`): giảng viên nhập điểm một lần theo vai trò của mình (hướng dẫn, phản biện hoặc hội đồng) cho một đồ án; hệ thống ghi cùng điểm đó cho mọi thành viên trong một lần lưu. **Chấm riêng** thì phải chọn sinh viên.
- **Đề tài** chỉ mở được trong đợt cùng loại; số sinh viên tối đa của đề tài không vượt quy mô nhóm của loại.

API `GET /api/v1/project-types` trả về các tham số **đang cấu hình** để giao diện hiển thị. Trọng số trả về dạng phân số, ví dụ `0.4`; `minComponentScore = null` nghĩa là đã tắt.

---

## 3. Vai trò và phân quyền

Mọi API đều yêu cầu đăng nhập, trừ `POST /auth/login`, `POST /auth/refresh` và `GET /health`.

**Danh tính luôn lấy từ JWT.** Server tự tra từ tài khoản đăng nhập ra hồ sơ giảng viên hoặc sinh viên tương ứng (`Common/CurrentActor.cs`). Client không gửi `lecturerId` hay `studentId` để "đóng vai" người khác.

| Chức năng | Admin (`ADMIN`) | Giảng viên (`GIANG_VIEN`) | Sinh viên (`SINH_VIEN`) |
|---|---|---|---|
| Danh mục học vụ, tài khoản | Toàn quyền | Xem | Xem |
| Đợt đồ án | Toàn quyền | Xem | Xem |
| Đề tài | Toàn quyền | Đề xuất (đứng tên mình); sửa đề tài **của mình**; duyệt đề tài **của người khác**; duyệt đề tài sinh viên đề xuất mà mình là **GV dự kiến hướng dẫn** | Xem; **đề xuất đề tài** kèm giảng viên dự kiến hướng dẫn |
| Đơn đăng ký | Xem và xử lý mọi đơn; **duyệt / từ chối yêu cầu đổi đề tài, đổi nhóm** | Xem mọi đơn; duyệt / từ chối nguyện vọng thuộc **đề tài mình ra** | Tạo đơn (tự làm trưởng nhóm); chỉ xem đơn mình tham gia; chỉ **trưởng nhóm** được sửa; gửi **yêu cầu đổi đề tài** (trưởng nhóm) hoặc **đổi nhóm** (thành viên) sau khi đơn đã duyệt |
| Đồ án | Tạo, phân công hướng dẫn | Xem; nhận xét tiến độ, yêu cầu chỉnh sửa nếu **đang hướng dẫn** | Chỉ đồ án của mình; nộp báo cáo tiến độ, tài liệu |
| Phản biện, hội đồng, lịch bảo vệ | Toàn quyền | Xem | Xem |
| Chấm điểm | Công bố kết quả | Chỉ chấm **đúng vai trò**: hướng dẫn, phản biện được phân công, hoặc thành viên hội đồng có lịch bảo vệ đồ án | Chỉ xem kết quả của mình **sau khi công bố** |

Một số ràng buộc khác:
- Khóa tài khoản hoặc đổi vai trò thì token cũ mất hiệu lực ngay ở request tiếp theo.
- Admin không tự khóa hoặc tự đổi vai trò của chính mình.
- Giảng viên hướng dẫn không được làm phản biện cho đồ án mình hướng dẫn.
- Hội đồng bảo vệ theo quy chế có **3 đến 5 thành viên**, gồm **đúng một Chủ tịch**, **đúng một Thư ký**, còn lại là Ủy viên. Chỉ xếp (hoặc sửa) lịch bảo vệ khi hội đồng đủ điều kiện. Hội đồng đã có lịch thì không được bớt thành viên làm mất điều kiện. CSDL có index unique riêng cho Chủ tịch và Thư ký.
- Kết quả đã công bố thì khóa điểm.
- Đăng nhập giới hạn 10 lần/phút cho mỗi IP.

---

## 4. Quy trình nghiệp vụ

```
Admin mở đợt (chọn loại đồ án) ──► thêm các lớp tham gia ──► chuyển trạng thái DANG_MO_DANG_KY
        │
Giảng viên đề xuất đề tài (CHO_DUYET) ──► GV khác / Admin duyệt (DA_DUYET) ──► Admin mở đề tài trong đợt cùng loại
   hoặc Sinh viên đề xuất đề tài + chọn GV dự kiến hướng dẫn (CHO_DUYET) ──► chính GV đó / Admin duyệt
        │
Sinh viên tạo nhóm (NHAP, tự làm trưởng nhóm) ──► thêm thành viên, chọn nguyện vọng ──► gửi duyệt (CHO_DUYET)
        │        (đợt phải đang mở, chưa quá hạn đăng ký, lớp thuộc đợt, đủ điều kiện của loại đồ án)
        │
GV ra đề tài duyệt một nguyện vọng (DA_DUYET; các nguyện vọng khác tự bị từ chối)
        │
(Tùy chọn) SV gửi yêu cầu đổi đề tài / đổi nhóm kèm lý do ──► Admin duyệt (áp dụng vào đơn và đồ án) hoặc từ chối
        │
Admin tạo đồ án + phân công GV hướng dẫn ──► SV nộp báo cáo tiến độ, tài liệu ──► GV nhận xét, yêu cầu chỉnh sửa
        │
(Loại có phản biện: chuyên ngành, tốt nghiệp) Admin phân công phản biện / đọc duyệt
        │
Admin lập hội đồng 3 - 5 người (1 Chủ tịch, 1 Thư ký), xếp lịch bảo vệ
        │        (trong khoảng bảo vệ của đợt, không trùng phòng / hội đồng)
        │
Các GV chấm điểm theo vai trò (riêng từng SV hoặc chung cả nhóm) ──► đủ cột điểm thì hệ thống tự tính tổng kết ──► Admin công bố kết quả
```

Trạng thái đợt đồ án: `CHUA_MO` → `DANG_MO_DANG_KY` → `DANG_THUC_HIEN` → `DANG_BAO_VE` → `DA_KET_THUC` (hoặc `DA_HUY`).
Trạng thái đồ án: `CHO_THUC_HIEN`, `DANG_THUC_HIEN`, `CHO_BAO_VE`, `DA_BAO_VE`, `HOAN_THANH`, `TAM_DUNG`, `DA_HUY`.

### Yêu cầu đổi đề tài / đổi nhóm

Sau khi đơn đăng ký đã duyệt, sinh viên có thể xin thay đổi; Admin duyệt hoặc từ chối (kèm lý do). Mỗi sinh viên chỉ có một yêu cầu chờ duyệt tại một thời điểm.

| Loại | Ai gửi | Khi Admin duyệt |
|---|---|---|
| `DOI_DE_TAI` | Trưởng nhóm, kèm `newPeriodTopicId` | Nguyện vọng đang được chọn chuyển sang đề tài mới; đồ án của đơn (nếu đã tạo) đổi đề tài |
| `DOI_NHOM` | Một thành viên, kèm `targetRegistrationId` | Sinh viên rời nhóm cũ sang nhóm mới, ở cả đơn đăng ký và đồ án (nếu đã tạo). Trưởng nhóm rời đi thì người vào sớm nhất trong số còn lại làm trưởng nhóm; nhóm không còn ai thì đơn và đồ án chuyển `DA_HUY` |

Khi duyệt, hệ thống kiểm tra lại các quy tắc:
- **Đề tài mới:** cùng đợt và đang mở, chưa giao cho nhóm khác, còn đủ sức chứa.
- **Nhóm mới:** cùng đợt, đã duyệt, chưa vượt quy mô nhóm của loại đồ án và sức chứa của đề tài.
- **Sinh viên:** chỉ thuộc một nhóm trong đợt.

**Không cho đổi khi kết quả của đồ án liên quan đã công bố.** Riêng đổi nhóm còn bị chặn khi sinh viên đã có phiếu chấm ở đồ án cũ, vì điểm gắn với đồ án cũ.

---

## 5. Giao diện web

Frontend nằm trong `frontend/`. Trang chạy theo hash route (`#/dashboard`...), menu bên trái thay đổi theo vai trò đăng nhập.

**Trang đăng nhập:**
- Bố cục hai cột: bên trái (65%) là ảnh khu giảng đường kèm logo trường, bên phải (35%) là form đăng nhập.
- Màn hình hẹp (dưới 900px) chỉ còn form.
- Link "Quên mật khẩu?" hiện hướng dẫn liên hệ giảng viên hướng dẫn, vì hệ thống chưa có chức năng đặt lại mật khẩu.

| Vai trò | Các trang |
|---|---|
| Admin | Tổng quan; Khoa & Bộ môn, Giảng viên, Sinh viên; Đợt đồ án, Đề tài, Duyệt đề tài, Đăng ký, Đồ án thực hiện; Hội đồng & Lịch, Bảng điểm tổng kết; Thông báo, Quản trị tài khoản |
| Giảng viên | Tổng quan; Danh mục đề tài, Đề xuất đề tài, Duyệt đề tài, Đồ án hướng dẫn, Đăng ký chờ xử lý; Phản biện & Hội đồng, Chấm điểm đồ án, Kết quả đồ án; Giảng viên, Sinh viên; Thông báo |
| Sinh viên | Tổng quan; Đợt đồ án, Khám phá đề tài, Đề xuất đề tài, Đăng ký đề tài nhóm, Đồ án của tôi; Lịch bảo vệ, Kết quả đồ án; Thông báo |

**Tùy biến:**
- Tên ứng dụng, logo và địa chỉ API: `frontend/js/config.js` (`APP_NAME`, `APP_LOGO`, `API_BASE_URL`).
- Ảnh và logo trang đăng nhập: `frontend/js/pages/login.js` (`CAMPUS_IMAGE`, `SCHOOL_LOGO`).
- Favicon: `frontend/assets/favicon.ico`.

---

## 6. Cấu trúc dự án

```
├── backend/
│   ├── UniProject.slnx
│   ├── UniProject.API/                     Web API (toàn bộ backend)
│   │   ├── Program.cs                      Cấu hình DI, JWT, phân quyền, CORS, rate limit, migration + seed khi khởi động
│   │   ├── .env.example                    Mẫu cấu hình (chép thành .env, không commit .env)
│   │   ├── appsettings.json                Logging, tham số các loại đồ án (ProjectTypes)
│   │   ├── Common/                         CurrentActor, xử lý lỗi, lưu file (uploads/), validation, nạp .env
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs             DbContext duy nhất cho mọi module
│   │   │   ├── CrossModuleRelationships.cs Khóa ngoại giữa các module
│   │   │   ├── Migrations/
│   │   │   ├── DbInitializer.cs            Chạy migration; seed cơ cấu, giảng viên, sinh viên, Đồ án cơ sở K65
│   │   │   ├── CourseProjectSeeder.cs      Seed Đồ án môn học (Công nghệ Java, Lập trình trực quan)
│   │   │   ├── ThesisSeeder.cs             Seed Đồ án tốt nghiệp (đọc duyệt, hội đồng, điểm, báo cáo PDF, thông báo)
│   │   │   ├── SeedFiles.cs                Tìm file seed: Data/Seed/ trước, rồi Data/Seed.example/
│   │   │   ├── SampleAcademicStructure.cs  Khoa, bộ môn, ngành, lớp của UTC2
│   │   │   ├── SampleK65CnttProjects.cs    Mốc thời gian, mô tả đề tài Đồ án cơ sở K65
│   │   │   ├── SampleK65CourseProjects.cs  Mốc thời gian, đề tài, yêu cầu của các đồ án môn học
│   │   │   ├── Seed/                       Dữ liệu thật — KHÔNG commit (xem mục 10)
│   │   │   └── Seed.example/               Dữ liệu hư cấu cùng định dạng — có trong Git
│   │   └── Modules/
│   │       ├── Identity/                   Tài khoản, đăng nhập, JWT
│   │       ├── Academic/                   Khoa, bộ môn, chuyên ngành, lớp, năm học, học kỳ, giảng viên, sinh viên
│   │       ├── ProjectPeriod/              Đợt đồ án + ProjectTypes/ (luật của 4 loại đồ án)
│   │       ├── Topic/                      Đề tài
│   │       ├── Registration/               Đăng ký nhóm, nguyện vọng, yêu cầu đổi đề tài / đổi nhóm
│   │       ├── Project/                    Đồ án, hướng dẫn, tiến độ, tài liệu, yêu cầu chỉnh sửa
│   │       ├── Defense/                    Phản biện, hội đồng, lịch bảo vệ
│   │       ├── Grading/                    Phiếu chấm, kết quả, công thức tính điểm
│   │       └── Notification/               Thông báo
│   └── UniProject.Share/                   Thư viện dùng chung: Result, phân trang, exception, hằng số vai trò
└── frontend/
    ├── index.html
    ├── assets/                             Logo ứng dụng, favicon, logo trường, ảnh trang đăng nhập
    ├── css/                                base (biến màu, nút, form, bảng, badge), layout (sidebar, topbar), components, pages
    └── js/
        ├── config.js                       Tên ứng dụng, logo, địa chỉ API
        ├── main.js                         Khởi động, render trang theo hash route
        ├── core/                           api (fetch + token + refresh), auth, router, toast, format, lookup
        ├── components/                     shell (sidebar, topbar, panel thông báo), ui (modal, phân trang, tab...)
        └── pages/                          Mỗi trang một file
```

Mỗi module có cùng bố cục: `Entities/`, `Configurations/` (cấu hình bảng EF), `Dtos/`, `Services/`, `Controllers/`, và file `<Tên>Module.cs` đăng ký service của module. Mỗi module dùng một namespace riêng (`UniProject.API.Modules.<Tên>`).

**Ranh giới module:** entity của một module chỉ giữ *mã* (int) của module khác, không có navigation property sang module khác. Toàn vẹn dữ liệu vẫn được đảm bảo bằng khóa ngoại thật trong CSDL, khai báo tập trung ở `Data/CrossModuleRelationships.cs`. Tất cả các khóa ngoại này đều `Restrict`, nên không xóa được bản ghi đang được tham chiếu.

---

## 7. Design pattern sử dụng

| Pattern | Ở đâu | Mục đích |
|---|---|---|
| **Modular Monolith** | `Modules/*` | Tách nghiệp vụ thành module rõ ràng nhưng triển khai một ứng dụng duy nhất |
| **Strategy** | `IProjectTypePolicy`, `DoAnCoSoPolicy`, `DoAnChuyenNganhPolicy`, `DoAnMonHocPolicy`, `DoAnTotNghiepPolicy` | Luật khác nhau giữa các loại đồ án; thêm loại mới không phải sửa service |
| **Factory / Resolver** | `ProjectTypePolicyResolver` | Chọn đúng policy theo loại đồ án của đợt / đồ án |
| **Template formula** | `ProjectGradeCalculator` | Một công thức tính điểm chung; trọng số và ngưỡng đạt lấy từ policy |
| **Options + validation khi khởi động** | `ProjectTypesOptions`, `ProjectTypesOptionsValidator` | Tham số loại đồ án đọc từ cấu hình; sai thì dừng ứng dụng |
| **Rich Domain Model** | `Modules/*/Entities` | Entity tự bảo vệ trạng thái hợp lệ (ví dụ chỉ duyệt được đề tài đang chờ duyệt) |
| **Unit of Work** | `AppDbContext.SaveChangesAsync` | Ví dụ phiếu chấm và kết quả luôn được lưu trong cùng một transaction |
| **Optimistic Concurrency** | Cột `RowVersion` trên mọi bảng nghiệp vụ | Hai người sửa cùng lúc thì người sau nhận lỗi 409 thay vì ghi đè |
| **Global Exception Handler** | `Common/GlobalExceptionHandler.cs` | Service chỉ cần throw exception; lỗi được chuyển thành HTTP status + JSON thống nhất |
| **Action Filter** | `Common/ValidationFilter.cs` | Tự chạy FluentValidation trước khi vào controller |

---

## 8. Cài đặt và cấu hình chi tiết

### Yêu cầu
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- SQL Server: bản cài đặt đầy đủ, SQL Server Express, hoặc LocalDB (có sẵn khi cài Visual Studio)
- (Tùy chọn) công cụ EF: `dotnet tool install --global dotnet-ef`
- (Tùy chọn) Python hoặc VS Code Live Server để phục vụ frontend

### Cấu hình backend

Cấu hình nhạy cảm đặt trong file `.env`, **không commit lên Git**. Tạo file này từ bản mẫu:

```bash
cp backend/UniProject.API/.env.example backend/UniProject.API/.env
```

| Biến | Ý nghĩa |
|---|---|
| `ConnectionStrings__DefaultConnection` | Chuỗi kết nối SQL Server. Ví dụ LocalDB: `Server=(localdb)\MSSQLLocalDB;Database=QLDoAnCoSoChuyenNganh;Trusted_Connection=True;TrustServerCertificate=True` |
| `Jwt__Key` | Khóa ký JWT, **tối thiểu 32 ký tự**, dùng chuỗi ngẫu nhiên |
| `Jwt__Issuer`, `Jwt__Audience` | Định danh phát hành / sử dụng token |
| `Jwt__ExpiryMinutes`, `Jwt__RefreshTokenExpiryDays` | Thời hạn access token / refresh token |
| `SeedSampleData` | `true` để tạo tài khoản demo và dữ liệu mẫu khi khởi động |
| `RateLimit__LoginPerMinute` | (Tùy chọn) số lần gọi đăng nhập mỗi phút cho mỗi IP, mặc định 10 |
| `ProjectTypes__<LOAI>__<Khóa>` | (Tùy chọn) tham số loại đồ án, xem [mục 2](#2-các-loại-đồ-án); mặc định nằm trong `appsettings.json` |
| `Cors__AllowedOrigins__0`, `__1`... | (Tùy chọn) origin của frontend; mặc định cho phép `http://localhost:5500` và `http://127.0.0.1:5500` |

Biến môi trường thật của hệ điều hành / Docker / server được ưu tiên hơn giá trị trong `.env`.

### Chạy backend

```bash
cd backend/UniProject.API
dotnet run
```

- Khi khởi động, ứng dụng **tự chạy migration**, tức là tự tạo database nếu chưa có. Nếu migration lỗi hoặc không kết nối được SQL Server, ứng dụng dừng hẳn và in hướng dẫn kiểm tra.
- Swagger UI (chỉ ở môi trường Development): `http://localhost:5000/swagger`. Đăng nhập bằng `POST /api/v1/auth/login`, copy `accessToken` rồi bấm **Authorize**.
- Health check: `GET http://localhost:5000/health`.
- File sinh viên tải lên (báo cáo, slide...) lưu ở `backend/UniProject.API/uploads/projects/{mã đồ án}/`. CSDL chỉ lưu đường dẫn tương đối.

### Chạy frontend

Frontend dùng ES modules nên phải mở qua một web server tĩnh, không mở trực tiếp bằng `file://`. Có hai cách:

- **VS Code Live Server:** chuột phải `frontend/index.html` → *Open with Live Server* (cổng mặc định 5500).
- **Python:**

  ```bash
  cd frontend
  python -m http.server 5500
  ```
  Sau đó mở `http://localhost:5500`.

Địa chỉ API cấu hình trong `frontend/js/config.js`, mặc định `http://localhost:5000/api/v1`. Chạy frontend ở cổng khác thì thêm origin qua biến `Cors__AllowedOrigins__0=...` trong `.env`.

### Tạo lại database từ đầu

```bash
cd backend/UniProject.API
dotnet ef database drop -f
dotnet run
```

---

## 9. Dữ liệu mẫu và tài khoản demo

Dữ liệu mẫu chỉ được tạo khi `SeedSampleData=true`.

**Cơ cấu đào tạo** nằm trong code (`Data/SampleAcademicStructure.cs`), theo UTC2 và tập trung vào Bộ môn CNTT:
- 4 khoa / 16 bộ môn cùng nhóm "Giảng viên thỉnh giảng", 22 ngành tuyển sinh năm 2025 kèm chuyên ngành.
- 91 lớp: đủ các lớp của mọi ngành từ K62 đến K65, thêm lớp CNTT K61, K66, K67 và lớp cử nhân `CQ.62.CN.CNTT`, `CQ.63.CN.CNTT`. Lớp còn thiếu (vd `CQ.59.CNTT`, `CQ.60.CNTT`) được tạo thêm khi seed Đồ án tốt nghiệp.
- Năm học 2024-2025 đến 2026-2027, mỗi năm 2 học kỳ.

**Giảng viên, sinh viên và các đợt đồ án** lấy từ file JSON. Mỗi file được tìm ở `Data/Seed/` trước (dữ liệu thật, không commit), không có thì dùng file cùng tên trong `Data/Seed.example/` (dữ liệu hư cấu, có trong Git). Định dạng từng file mô tả ở `backend/UniProject.API/Data/Seed.example/README.md`.

| File | Dùng để seed |
|---|---|
| `lecturers.json` | Giảng viên cơ hữu (mã `GV001`... theo thứ tự trong file, `GV001` là Trưởng Bộ môn) và thỉnh giảng (mã `TG001`...) |
| `students.json` | Sinh viên theo lớp |
| `k65-do-an-co-so.json` | Đợt Đồ án cơ sở K65 |
| `k65-do-an-mon-hoc.json` | Đợt Đồ án môn học Công nghệ Java và Lập trình trực quan |
| `datn-2026-03.json` (+ thư mục `datn-2026-03/` chứa báo cáo PDF) | Đợt Đồ án tốt nghiệp giao tháng 03/2026 |

Các đợt đồ án được seed:

| Mã đợt | Loại | Trạng thái | Dữ liệu kèm theo |
|---|---|---|---|
| `DACS-K65-HK1-2026` | Đồ án cơ sở | Đang thực hiện | Đề tài đã duyệt (mục tiêu, yêu cầu, công nghệ), nhóm đã duyệt, phân công GVHD |
| `DAMH-JAVA-K65-2025` | Đồ án môn học (Công nghệ Java) | Đã kết thúc | Nhóm tự chia tối đa 4 SV, 3 lần báo cáo tiến độ có nhận xét, hội đồng, lịch bảo vệ, phiếu chấm, kết quả đã công bố |
| `DAMH-LTTQ-K65-2026` | Đồ án môn học (Lập trình trực quan) | Đang thực hiện | Đề tài do sinh viên đề xuất, góp ý duyệt của GV dưới dạng yêu cầu chỉnh sửa |
| `DATN-03-2026` | Đồ án tốt nghiệp | Đã kết thúc | Phiếu nhiệm vụ, 4 lần báo cáo tiến độ, đọc duyệt, 3 hội đồng, lịch bảo vệ, phiếu chấm, kết quả, yêu cầu chỉnh sửa sau bảo vệ, thông báo, báo cáo PDF (nếu có) |

Mốc thời gian các đợt theo kế hoạch đồ án tốt nghiệp đăng trên trang Bộ môn CNTT UTC2 (`dept.utc2.edu.vn/bomoncntt`). Mỗi đợt chỉ được seed một lần theo mã đợt, cơ cấu chỉ seed khi database chưa có khoa nào. Seed không ghi đè dữ liệu đã có.

Với dữ liệu mẫu trong `Data/Seed.example/` có 14 giảng viên, 96 sinh viên và đủ cả 4 đợt đồ án ở quy mô nhỏ.

Tài khoản sinh viên seed được băm BCrypt với work factor 4 để seed nhanh (khoảng 30 giây cho 6.000 tài khoản). Lần đăng nhập đầu tiên hệ thống tự băm lại với work factor 12.

| Đối tượng | Tài khoản | Mật khẩu | Email |
|---|---|---|---|
| Admin | `admin` | `Admin@123456` | admin@utc2.edu.vn |
| Giảng viên | `username` trong `lecturers.json` (dữ liệu mẫu: `gv01` ... `gv09`, `tg01` ... `tg05`) | `Lecturer@123456` | `<tài khoản>@utc2.edu.vn` |
| Sinh viên | MSSV trong `students.json` (dữ liệu mẫu: vd `6599071001`) | MSSV | `<MSSV>@st.utc2.edu.vn` |

Trên môi trường thật, hãy đặt `SeedSampleData=false` và **đổi mật khẩu các tài khoản demo**.

---

## 10. Bảo vệ dữ liệu khi đưa lên Git

Repo không chứa dữ liệu cá nhân hay bí mật nào. Các mục sau đã được loại trong `.gitignore`:

| Không commit | Lý do |
|---|---|
| `backend/UniProject.API/.env` | Chuỗi kết nối CSDL, khóa ký JWT |
| `backend/UniProject.API/Data/Seed/` | Dữ liệu thật: họ tên, MSSV, giảng viên, đề tài, báo cáo đồ án tốt nghiệp |
| `backend/UniProject.API/uploads/` | File người dùng tải lên |
| `bin/`, `obj/`, `.vs/`, `*.user`, `*.log` | File build, cấu hình cá nhân của IDE, log |

Nguyên tắc khi đóng góp:
- **Không đưa dữ liệu thật vào code.** Họ tên, MSSV, email, số điện thoại, tài khoản giảng viên phải nằm trong file JSON ở `Data/Seed/`. Code chỉ chứa luật nghiệp vụ, mốc thời gian và mô tả chung.
- Khi thêm loại file seed mới, thêm luôn một bản hư cấu cùng tên vào `Data/Seed.example/`, để người khác clone về vẫn chạy được.
- Dữ liệu hư cấu dùng MSSV dạng `xx99071xxx` (mã trường `99` không tồn tại) và tài khoản giảng viên `gv01`, `tg01`...
- Trước khi commit, chạy `git status` và kiểm tra không có file nào trong `Data/Seed/`, `uploads/` hoặc file `.env`.

Muốn dùng dữ liệu thật trên máy khác: chép thư mục `Data/Seed/` sang đúng vị trí, rồi tạo lại database (xem [mục 8](#tạo-lại-database-từ-đầu)).

---

## 11. Danh sách API

Tiền tố chung là `/api/v1`. Chi tiết tham số xem ở Swagger.

| Nhóm | Route | Ghi chú |
|---|---|---|
| Xác thực | `/auth` | `login`, `refresh`, `me`, `change-password`; quản lý tài khoản `users/*` (Admin) |
| Học vụ | `/faculties`, `/departments`, `/majors`, `/classes`, `/academic-years`, `/semesters`, `/degrees` | Danh mục |
| Giảng viên / Sinh viên | `/lecturers`, `/students` | Có `GET /lecturers/me`, `GET /students/me` để lấy hồ sơ của chính mình |
| Loại đồ án | `/project-types` | Tham số đang cấu hình của 4 loại đồ án |
| Đợt đồ án | `/project-periods` | Lọc theo `projectKind`; đổi trạng thái; thêm / bớt lớp |
| Đề tài | `/topics` | Lọc theo `projectKind`; duyệt / từ chối; mở đề tài trong đợt (`/topics/periods`); sinh viên đề xuất `POST /topics/student-proposals` |
| Đăng ký | `/registrations`, `/registration-preferences` | `GET /registrations/me` cho sinh viên |
| Đổi đề tài / đổi nhóm | `/registration-change-requests` | SV: `POST`, `GET /me`; Admin: `GET` (lọc `periodId`, `status`), `POST /{id}/approve`, `POST /{id}/reject` |
| Đồ án | `/projects`, `/progress-reports`, `/revision-requests` | Tài liệu: upload `POST /projects/{id}/documents`, tải `GET /projects/{id}/documents/{docId}/file` |
| Bảo vệ | `/defenses` | Phản biện, hội đồng, lịch bảo vệ |
| Chấm điểm | `/projects/{projectId}/grades`, `/projects/{projectId}/results`, `/projects/{projectId}/publish-result` | `POST /grades` trả về **danh sách** phiếu đã ghi; `studentId` bỏ trống được khi chấm chung |
| Thông báo | `/notifications` | |

---

## 12. Quy ước phản hồi và mã lỗi

Mọi phản hồi, kể cả lỗi, đều có dạng:

```json
{ "success": false, "message": "Mô tả lỗi", "errors": { "Field": ["Chi tiết"] }, "data": null }
```

| Mã | Khi nào |
|---|---|
| `400` | Dữ liệu không hợp lệ, hoặc vi phạm luật nghiệp vụ (quá hạn, sai trạng thái, sai loại đồ án...) |
| `401` | Chưa đăng nhập, token hết hạn, hoặc tài khoản đã bị khóa |
| `403` | Đã đăng nhập nhưng không có quyền với dữ liệu này |
| `404` | Không tìm thấy bản ghi |
| `409` | Trùng dữ liệu, dữ liệu đang được tham chiếu, hoặc bị người khác sửa đồng thời |
| `429` | Gọi đăng nhập quá nhiều lần |
