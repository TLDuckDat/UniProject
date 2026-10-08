# Quản lý Đồ án cơ sở & Đồ án chuyên ngành — Backend

Backend REST API quản lý toàn bộ vòng đời **Đồ án cơ sở** và **Đồ án chuyên ngành**: mở đợt, đề xuất (giảng viên hoặc sinh viên) và duyệt đề tài, đăng ký nhóm, đổi đề tài / đổi nhóm sau khi duyệt, thực hiện đồ án (báo cáo tiến độ, tài liệu, yêu cầu chỉnh sửa), phản biện, hội đồng và lịch bảo vệ, chấm điểm và công bố kết quả.

- **Nền tảng:** .NET 10, ASP.NET Core Web API, Entity Framework Core 10, SQL Server
- **Kiến trúc:** Modular Monolith, mỗi nghiệp vụ là một module độc lập trong cùng một project
- **Bảo mật:** JWT (access token + refresh token xoay vòng), phân quyền theo vai trò và theo quyền sở hữu dữ liệu

---

## Mục lục

1. [Các loại đồ án](#1-các-loại-đồ-án)
2. [Vai trò và phân quyền](#2-vai-trò-và-phân-quyền)
3. [Quy trình nghiệp vụ](#3-quy-trình-nghiệp-vụ)
4. [Cấu trúc dự án](#4-cấu-trúc-dự-án)
5. [Design pattern sử dụng](#5-design-pattern-sử-dụng)
6. [Cài đặt và chạy](#6-cài-đặt-và-chạy)
7. [Danh sách API](#7-danh-sách-api)
8. [Quy ước phản hồi và mã lỗi](#8-quy-ước-phản-hồi-và-mã-lỗi)

---

## 1. Các loại đồ án

Mỗi **đợt đồ án** và mỗi **đề tài** thuộc đúng một loại. Luật của từng loại được đóng gói trong một policy riêng (`Modules/ProjectPeriod/ProjectTypes`), các service nghiệp vụ chỉ hỏi policy, không viết `if/else` theo loại.

**Đồ án môn học** (`DO_AN_MON_HOC`) dùng cho đồ án của một học phần (vd Công nghệ Java, Lập trình trực quan): nhóm tối đa 4 sinh viên, không có phản biện, không có điều kiện tham gia. **Đồ án tốt nghiệp** (`DO_AN_TOT_NGHIEP`): mỗi sinh viên một đề tài, có giảng viên đọc duyệt (phản biện), điểm theo đúng công thức 0,4 × quá trình (hướng dẫn 20% + đọc duyệt 20%) + 0,6 × bảo vệ của quy chế.

Quy chế đào tạo của trường (QĐ 2006/QĐ-ĐHGTVT ngày 05/11/2021) quy định công thức điểm đồ án tốt nghiệp là 0,4 × điểm quá trình (hướng dẫn, đọc duyệt) + 0,6 × điểm bảo vệ. Các trọng số mặc định dưới đây tương đương với quy chế này. Quy chế **không** quy định riêng cho đồ án cơ sở / chuyên ngành về quy mô nhóm, có phản biện hay không, mức điểm đạt, chấm riêng hay chung, nên các tham số này **cấu hình được, không cần sửa mã**.

Bảng dưới là **giá trị mặc định** (cấu hình trong `appsettings.json`, mục `ProjectTypes`):

| Tham số (khóa cấu hình) | Đồ án cơ sở (`DO_AN_CO_SO`) | Đồ án chuyên ngành (`DO_AN_CHUYEN_NGANH`) | Đồ án môn học (`DO_AN_MON_HOC`) | Đồ án tốt nghiệp (`DO_AN_TOT_NGHIEP`) |
|---|---|---|---|---|
| Quy mô nhóm tối đa (`MaxMembers`) | **2** sinh viên | **3** sinh viên | **4** sinh viên | **1** sinh viên |
| Giảng viên phản biện (`HasReviewer`) | **Không có** (`false`) | **Có** (`true`) | **Không có** (`false`) | **Có** (đọc duyệt) |
| Trọng số % hướng dẫn / phản biện / hội đồng (`AdvisorWeightPercent`, `ReviewerWeightPercent`, `CommitteeWeightPercent`) | 40 / 0 / 60 | 20 / 20 / 60 | 40 / 0 / 60 | 20 / 20 / 60 |
| Điểm tổng kết tối thiểu để đạt (`PassingScore`) | 5 | 5 | 5 | 5 |
| Mức tối thiểu mỗi cột điểm bắt buộc (`MinComponentScore`, `0` = tắt) | 4 | 4 | 4 | 4 |
| Chế độ chấm (`GradingMode`) | `CHAM_RIENG` | `CHAM_RIENG` | `CHAM_RIENG` | `CHAM_RIENG` |
| Điều kiện tham gia (cố định trong mã) | Không | Sinh viên phải **đã đạt** một Đồ án cơ sở (kết quả đã công bố) | Không | Không (Phòng Đào tạo xét) |

**Đổi cấu hình:** sửa `appsettings.json`, hoặc đặt biến môi trường / dòng trong `.env` theo dạng `ProjectTypes__<LOAI>__<Khóa>`, ví dụ:

```bash
ProjectTypes__DO_AN_CO_SO__MaxMembers=3
ProjectTypes__DO_AN_CO_SO__GradingMode=CHAM_CHUNG
ProjectTypes__DO_AN_CHUYEN_NGANH__MinComponentScore=0
```

Khóa nào không khai báo thì giữ giá trị mặc định. Cấu hình được kiểm tra khi khởi động; sai thì ứng dụng **dừng** và in danh sách lỗi. Các điều kiện kiểm tra:
- `MaxMembers` từ 1 đến 10 (khớp ràng buộc `CK_DETAI_SoLuongSinhVien` của bảng đề tài);
- ba trọng số không âm và tổng bằng **100**; `HasReviewer = false` thì `ReviewerWeightPercent` phải bằng 0, `HasReviewer = true` thì phải lớn hơn 0;
- `PassingScore` trong (0, 10]; `MinComponentScore` từ 0 đến 10; `GradingMode` là `CHAM_RIENG` hoặc `CHAM_CHUNG`.

Thay đổi có hiệu lực sau khi khởi động lại ứng dụng. Nên đổi trước khi mở đợt, vì điểm tổng kết được tính lại theo cấu hình đang chạy mỗi lần nhập điểm.

Luật chung cho cả hai loại:
- **Xếp loại** (cố định): từ 9 là Xuất sắc, từ 8 là Giỏi, từ 6.5 là Khá, từ 5 là Trung bình, còn lại là Yếu.
- **Không đạt** nếu điểm tổng kết dưới `PassingScore`, hoặc có cột điểm bắt buộc (cột có trọng số > 0) dưới `MinComponentScore`.
- **Chưa đủ cột điểm bắt buộc** thì kết quả là `CHO_KET_QUA`, chưa có tổng kết và không công bố được.
- **Chấm chung** (`CHAM_CHUNG`): giảng viên nhập điểm một lần theo vai trò của mình (hướng dẫn / phản biện / hội đồng) cho một đồ án, hệ thống ghi cùng điểm đó cho mọi thành viên trong cùng một lần lưu. **Chấm riêng** thì phải chọn sinh viên.
- **Đề tài:** chỉ mở được trong đợt cùng loại; số SV tối đa của đề tài không vượt quy mô nhóm của loại.

API `GET /api/v1/project-types` trả về các tham số **đang cấu hình** để giao diện hiển thị (trọng số dạng phân số, ví dụ `0.4`; `minComponentScore = null` nghĩa là đã tắt).

## 2. Vai trò và phân quyền

Mọi API đều yêu cầu đăng nhập, trừ `POST /auth/login`, `POST /auth/refresh` và `GET /health`.

**Danh tính luôn lấy từ JWT.** Server tự tra từ tài khoản đăng nhập ra hồ sơ giảng viên / sinh viên tương ứng (`Common/CurrentActor.cs`). Client không gửi `lecturerId` hay `studentId` để "đóng vai" người khác.

| Chức năng | Admin (`ADMIN`) | Giảng viên (`GIANG_VIEN`) | Sinh viên (`SINH_VIEN`) |
|---|---|---|---|
| Danh mục học vụ, tài khoản | Toàn quyền | Xem | Xem |
| Đợt đồ án | Toàn quyền | Xem | Xem |
| Đề tài | Toàn quyền | Đề xuất (đứng tên mình), sửa đề tài **của mình**, duyệt đề tài **của người khác**; duyệt đề tài sinh viên đề xuất mà mình là **GV dự kiến hướng dẫn** | Xem; **đề xuất đề tài** kèm giảng viên dự kiến hướng dẫn |
| Đơn đăng ký | Xem, xử lý mọi đơn; **duyệt / từ chối yêu cầu đổi đề tài, đổi nhóm** | Xem mọi đơn; duyệt / từ chối nguyện vọng thuộc **đề tài mình ra** | Tạo đơn (tự làm trưởng nhóm); chỉ xem đơn mình tham gia; chỉ **trưởng nhóm** được sửa; gửi **yêu cầu đổi đề tài** (trưởng nhóm) hoặc **đổi nhóm** (thành viên) sau khi đơn đã duyệt |
| Đồ án | Tạo, phân công hướng dẫn | Xem; nhận xét tiến độ, yêu cầu chỉnh sửa nếu **đang hướng dẫn** | Chỉ đồ án của mình; nộp báo cáo, tài liệu |
| Phản biện, hội đồng, lịch bảo vệ | Toàn quyền | Xem | Xem |
| Chấm điểm | Công bố kết quả | Chỉ chấm **đúng vai trò**: hướng dẫn, phản biện được phân công, hoặc thành viên hội đồng có lịch bảo vệ đồ án | Chỉ xem kết quả của mình **sau khi công bố** |

Một số ràng buộc khác:
- Khoá tài khoản hoặc đổi vai trò thì token cũ mất hiệu lực ngay ở request tiếp theo.
- Admin không tự khoá hoặc tự đổi vai trò của chính mình.
- Giảng viên hướng dẫn không được làm phản biện cho đồ án mình hướng dẫn.
- Hội đồng bảo vệ theo quy chế: **3 đến 5 thành viên**, gồm **đúng một Chủ tịch**, **đúng một Thư ký**, còn lại là Ủy viên. Chỉ xếp (hoặc sửa) lịch bảo vệ khi hội đồng đủ điều kiện; hội đồng đã có lịch thì không được bớt thành viên làm mất điều kiện. CSDL có index unique riêng cho Chủ tịch và Thư ký.
- Kết quả đã công bố thì khoá điểm.
- Đăng nhập giới hạn 10 lần/phút cho mỗi IP.

## 3. Quy trình nghiệp vụ

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
(Tuỳ chọn) SV gửi yêu cầu đổi đề tài / đổi nhóm kèm lý do ──► Admin duyệt (áp dụng vào đơn và đồ án) hoặc từ chối
        │
Admin tạo đồ án + phân công GV hướng dẫn ──► SV nộp báo cáo tiến độ, tài liệu ──► GV nhận xét, yêu cầu chỉnh sửa
        │
(Loại có phản biện) Admin phân công phản biện ──► Admin lập hội đồng 3 - 5 người (1 chủ tịch, 1 thư ký), xếp lịch bảo vệ
        │        (trong khoảng bảo vệ của đợt, không trùng phòng / hội đồng)
        │
Các GV chấm điểm theo vai trò (riêng từng SV hoặc chung cả nhóm) ──► đủ cột điểm thì hệ thống tự tính tổng kết ──► Admin công bố kết quả
```

### Yêu cầu đổi đề tài / đổi nhóm

Sau khi đơn đăng ký đã duyệt, sinh viên có thể xin thay đổi; Admin duyệt hoặc từ chối (kèm lý do). Mỗi sinh viên chỉ có một yêu cầu chờ duyệt tại một thời điểm.

| Loại | Ai gửi | Khi Admin duyệt |
|---|---|---|
| `DOI_DE_TAI` | Trưởng nhóm, kèm `newPeriodTopicId` | Nguyện vọng đang được chọn chuyển sang đề tài mới; đồ án của đơn (nếu đã tạo) đổi đề tài |
| `DOI_NHOM` | Một thành viên, kèm `targetRegistrationId` | Sinh viên rời nhóm cũ sang nhóm mới, ở cả đơn đăng ký và đồ án (nếu đã tạo). Trưởng nhóm rời đi thì người còn lại vào sớm nhất làm trưởng nhóm; nhóm không còn ai thì đơn và đồ án chuyển `DA_HUY` |

Khi duyệt, hệ thống kiểm tra lại các quy tắc hiện có: đề tài mới cùng đợt và đang mở, chưa giao cho nhóm khác, đủ sức chứa; nhóm mới cùng đợt, đã duyệt, chưa vượt quy mô nhóm của loại đồ án và sức chứa đề tài; một sinh viên chỉ thuộc một nhóm trong đợt. **Không cho đổi khi kết quả của đồ án liên quan đã công bố.** Riêng đổi nhóm còn không cho phép khi sinh viên đã có phiếu chấm ở đồ án cũ (điểm gắn với đồ án cũ).

## 4. Cấu trúc dự án

```
├── backend/
│   ├── UniProject.slnx
│   ├── UniProject.API/                       Web API (toàn bộ backend)
│   │   ├── Program.cs                 Cấu hình DI, JWT, phân quyền, middleware
│   │   ├── Common/                    Thành phần dùng chung: CurrentActor, xử lý lỗi, lưu file, validation, .env
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs        DbContext duy nhất cho mọi module
│   │   │   ├── CrossModuleRelationships.cs   Khoá ngoại giữa các module
│   │   │   ├── DbInitializer.cs       Chạy migration + seed dữ liệu mẫu
│   │   │   └── Migrations/
│   │   └── Modules/
│   │       ├── Identity/              Tài khoản, đăng nhập, JWT
│   │       ├── Academic/              Khoa, bộ môn, chuyên ngành, lớp, năm học, học kỳ, giảng viên, sinh viên
│   │       ├── ProjectPeriod/         Đợt đồ án + ProjectTypes/ (luật Đồ án cơ sở / chuyên ngành)
│   │       ├── Topic/                 Đề tài
│   │       ├── Registration/          Đăng ký nhóm, nguyện vọng
│   │       ├── Project/               Đồ án, hướng dẫn, tiến độ, tài liệu, yêu cầu chỉnh sửa
│   │       ├── Defense/               Phản biện, hội đồng, lịch bảo vệ
│   │       ├── Grading/               Phiếu chấm, kết quả
│   │       └── Notification/          Thông báo
│   └── UniProject.Share/                     Thư viện dùng chung: Result, phân trang, exception, hằng số vai trò
└── frontend/                       Giao diện web (HTML, CSS, JavaScript thuần)
```

Mỗi module có cùng bố cục: `Entities/`, `Configurations/` (cấu hình bảng EF), `Dtos/`, `Services/`, `Controllers/`, và file `<Tên>Module.cs` đăng ký service của module. Mỗi module dùng một namespace (`UniProject.API.Modules.<Tên>`).

**Ranh giới module:** entity của một module chỉ giữ *mã* (int) của module khác, không có navigation property sang module khác. Tính toàn vẹn dữ liệu vẫn được đảm bảo bằng khoá ngoại thật trong CSDL, khai báo tập trung ở `Data/CrossModuleRelationships.cs`. Tất cả các khoá ngoại này đều `Restrict`, nên không xoá được bản ghi đang được tham chiếu.

## 5. Design pattern sử dụng

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

## 6. Cài đặt và chạy

### Yêu cầu
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- SQL Server: bản cài đặt đầy đủ, SQL Server Express, hoặc LocalDB (có sẵn khi cài Visual Studio)
- (Tuỳ chọn) công cụ EF: `dotnet tool install --global dotnet-ef`

### Cấu hình

Cấu hình nhạy cảm được đặt trong file `.env`, không commit lên Git. Tạo file này từ bản mẫu:

```bash
cp backend/UniProject.API/.env.example backend/UniProject.API/.env
```

| Biến | Ý nghĩa |
|---|---|
| `ConnectionStrings__DefaultConnection` | Chuỗi kết nối SQL Server. Ví dụ LocalDB: `Server=(localdb)\MSSQLLocalDB;Database=QLDoAnCoSoChuyenNganh;Trusted_Connection=True;TrustServerCertificate=True` |
| `Jwt__Key` | Khoá ký JWT, **tối thiểu 32 ký tự**, dùng chuỗi ngẫu nhiên |
| `Jwt__Issuer`, `Jwt__Audience` | Định danh phát hành / sử dụng token |
| `Jwt__ExpiryMinutes`, `Jwt__RefreshTokenExpiryDays` | Thời hạn access token / refresh token |
| `SeedSampleData` | `true` để tạo tài khoản demo và dữ liệu mẫu khi khởi động |
| `RateLimit__LoginPerMinute` | (tuỳ chọn) số lần gọi đăng nhập mỗi phút cho mỗi IP, mặc định 10 |
| `ProjectTypes__<LOAI>__<Khóa>` | (tuỳ chọn) tham số loại đồ án, xem [mục 1](#1-các-loại-đồ-án); mặc định nằm trong `appsettings.json` |

Biến môi trường thật của hệ điều hành / Docker / server được ưu tiên hơn giá trị trong `.env`.

### Chạy

```bash
cd backend/UniProject.API
dotnet run
```

- Khi khởi động, ứng dụng **tự chạy migration**, tức là tự tạo database nếu chưa có. Nếu migration lỗi, ứng dụng dừng hẳn.
- Swagger UI (chỉ ở môi trường Development): `http://localhost:5000/swagger`. Đăng nhập bằng `POST /api/v1/auth/login`, copy `accessToken` rồi bấm **Authorize**.
- Health check: `GET http://localhost:5000/health`

### Chạy frontend

Frontend nằm trong thư mục `frontend/`, viết bằng HTML, CSS và JavaScript thuần (ES modules), không cần cài gì hay build. Vì dùng ES modules nên phải mở qua một web server tĩnh, không mở trực tiếp `file://`. Có hai cách:

- **VS Code Live Server**: chuột phải `frontend/index.html` → *Open with Live Server* (cổng mặc định 5500).
- **Python**:

  ```bash
  cd frontend
  python -m http.server 5500
  ```
  Sau đó mở `http://localhost:5500`.

Địa chỉ API cấu hình trong `frontend/js/config.js` (mặc định `http://localhost:5000/api/v1`). Backend mặc định cho phép CORS từ `http://localhost:5500` và `http://127.0.0.1:5500`. Chạy frontend ở cổng khác thì thêm origin qua biến `Cors__AllowedOrigins__0=...` trong `.env`.

Cấu trúc frontend:

```
frontend/
├── index.html
├── css/            base (biến màu, nút, form, bảng, badge), layout (sidebar, topbar), components, pages
└── js/
    ├── config.js   địa chỉ API
    ├── main.js     khởi động, render trang theo hash (#/dashboard...)
    ├── core/       api (fetch + token + refresh), auth, router, toast, format, lookup
    ├── components/ shell (sidebar, topbar, panel thông báo), ui (modal, phân trang, tab...)
    └── pages/      mỗi trang một file
```

Muốn tạo lại database từ đầu:

```bash
cd backend/UniProject.API
dotnet ef database drop -f
dotnet run
```

### Dữ liệu mẫu và tài khoản demo (khi `SeedSampleData=true`)

Cơ cấu đào tạo theo Phân hiệu Trường ĐH GTVT tại TP.HCM (UTC2), tập trung vào Bộ môn CNTT, nằm trong code (`Data/SampleAcademicStructure.cs`):
- 4 khoa / 16 bộ môn cùng nhóm "Giảng viên thỉnh giảng", 22 ngành tuyển sinh năm 2025 kèm chuyên ngành.
- 91 lớp: đủ các lớp của mọi ngành từ K62 đến K65, thêm lớp CNTT K61, K66, K67 và lớp cử nhân `CQ.62.CN.CNTT`, `CQ.63.CN.CNTT`.
- Năm học 2024-2025 đến 2026-2027.

Giảng viên, sinh viên và các đợt đồ án lấy từ file JSON trong `backend/UniProject.API/Data/`:

| Thư mục | Nội dung | Git |
|---|---|---|
| `Data/Seed/` | **Dữ liệu thật** (giảng viên, sinh viên, đề tài, báo cáo PDF) | **Không commit** (có trong `.gitignore`), chép tay sang máy khác |
| `Data/Seed.example/` | Dữ liệu **hư cấu** cùng định dạng, xem `Data/Seed.example/README.md` | Có trong Git |

Mỗi file được tìm ở `Data/Seed/` trước, không có thì dùng file cùng tên trong `Data/Seed.example/`. Vì vậy clone về là chạy được ngay với dữ liệu hư cấu; muốn dùng dữ liệu thật thì đặt file vào `Data/Seed/`.

Các đợt đồ án được seed: Đồ án cơ sở K65 (đang thực hiện), Đồ án môn học Công nghệ Java (đã kết thúc, có điểm) và Lập trình trực quan (đang thực hiện), Đồ án tốt nghiệp đợt 03/2026 (đã bảo vệ, có đọc duyệt, hội đồng, điểm, thông báo).

Tài khoản sinh viên seed được băm BCrypt với work factor 4 để seed nhanh (khoảng 30 giây cho 6.000 tài khoản). Lần đăng nhập đầu tiên hệ thống tự băm lại với work factor 12.

| Đối tượng | Tài khoản | Mật khẩu | Email |
|---|---|---|---|
| Admin | `admin` | `Admin@123456` | admin@utc2.edu.vn |
| Giảng viên | `username` trong `lecturers.json` (dữ liệu mẫu: `gv01` ... `gv09`, `tg01` ... `tg05`) | `Lecturer@123456` | `<tài khoản>@utc2.edu.vn` |
| Sinh viên | MSSV trong `students.json` (dữ liệu mẫu: vd `6599071001`) | MSSV | `<MSSV>@st.utc2.edu.vn` |

Dữ liệu mẫu chỉ được tạo khi database chưa có khoa nào (các đợt đồ án: khi chưa có đợt cùng mã), không ghi đè dữ liệu đã có. Trên môi trường thật, hãy đặt `SeedSampleData=false`.

## 7. Danh sách API

Tiền tố chung là `/api/v1`. Chi tiết tham số xem ở Swagger.

| Nhóm | Route | Ghi chú |
|---|---|---|
| Xác thực | `/auth` | `login`, `refresh`, `me`, `change-password`; quản lý tài khoản `users/*` (Admin) |
| Học vụ | `/faculties`, `/departments`, `/majors`, `/classes`, `/academic-years`, `/semesters`, `/degrees` | Danh mục |
| Giảng viên / Sinh viên | `/lecturers`, `/students` | Có `GET /lecturers/me`, `GET /students/me` để lấy hồ sơ của chính mình |
| Loại đồ án | `/project-types` | Luật của Đồ án cơ sở / chuyên ngành |
| Đợt đồ án | `/project-periods` | Lọc theo `projectKind`; đổi trạng thái; thêm / bớt lớp |
| Đề tài | `/topics` | Lọc theo `projectKind`; duyệt / từ chối; mở đề tài trong đợt (`/topics/periods`); sinh viên đề xuất `POST /topics/student-proposals` |
| Đăng ký | `/registrations`, `/registration-preferences` | `GET /registrations/me` cho sinh viên |
| Đổi đề tài / đổi nhóm | `/registration-change-requests` | SV: `POST`, `GET /me`; Admin: `GET` (lọc `periodId`, `status`), `POST /{id}/approve`, `POST /{id}/reject` |
| Đồ án | `/projects`, `/progress-reports`, `/revision-requests` | Tài liệu: upload `POST /projects/{id}/documents`, tải `GET /projects/{id}/documents/{docId}/file` |
| Bảo vệ | `/defenses` | Phản biện, hội đồng, lịch bảo vệ |
| Chấm điểm | `/projects/{projectId}/grades`, `/projects/{projectId}/results`, `/projects/{projectId}/publish-result` | `POST /grades` trả về **danh sách** phiếu đã ghi; `studentId` bỏ trống được khi chấm chung |
| Thông báo | `/notifications` | |

## 8. Quy ước phản hồi và mã lỗi

Mọi phản hồi, kể cả lỗi, đều có dạng:

```json
{ "success": false, "message": "Mô tả lỗi", "errors": { "Field": ["Chi tiết"] }, "data": null }
```

| Mã | Khi nào |
|---|---|
| `400` | Dữ liệu không hợp lệ, hoặc vi phạm luật nghiệp vụ (quá hạn, sai trạng thái, sai loại đồ án...) |
| `401` | Chưa đăng nhập, token hết hạn, hoặc tài khoản đã bị khoá |
| `403` | Đã đăng nhập nhưng không có quyền với dữ liệu này |
| `404` | Không tìm thấy bản ghi |
| `409` | Trùng dữ liệu, dữ liệu đang được tham chiếu, hoặc bị người khác sửa đồng thời |
| `429` | Gọi đăng nhập quá nhiều lần |
