# UPM – Hệ thống Quản lý Đồ án tốt nghiệp (UTC / UTC2)

Quản lý toàn bộ quy trình ĐATN cho Trường ĐH Giao thông Vận tải (Hà Nội) và Phân hiệu TP.HCM.

## Công nghệ

| Phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10), EF Core + SQL Server, Serilog, Hangfire, Swagger |
| Frontend | Angular 21 (standalone, signals, zoneless), NG-ZORRO (vi_VN), ng-openapi-gen |
| Test | xUnit v3, Testcontainers (SQL Server), Vitest (Angular) |
| Hạ tầng dev | Docker Compose: SQL Server 2022, Mailpit |

## Yêu cầu cài đặt

- .NET SDK 10 (`global.json` ghim 10.0.4xx)
- Node.js 22 LTS (≥ 22.12) + npm
- Docker Desktop (đang chạy) – cần cho cả `docker compose` và integration test

## Chạy lần đầu

```bash
# 1. Hạ tầng: SQL Server (localhost:1433) + Mailpit (SMTP 1025, giao diện http://localhost:8025)
cp .env.example .env          # tùy chọn – đổi mật khẩu/port nếu cần
docker compose up -d

# 2. Backend
dotnet tool restore           # cài dotnet-ef (local tool)
dotnet build
dotnet test                   # cần Docker: integration test tự bật SQL Server bằng Testcontainers
dotnet run --project src/Upm.Api
#   → API:      http://localhost:5080
#   → Swagger:  http://localhost:5080/swagger
#   → Health:   http://localhost:5080/health
#   → Hangfire: http://localhost:5080/hangfire?access_token=<token của admin> (chỉ vai trò QuanTri)

# 3. Frontend (terminal khác)
cd web
npm ci
npm start                     # http://localhost:4200 – proxy /api sang http://localhost:5080
```

Mở http://localhost:4200 → trang đăng nhập. Đăng nhập bằng một tài khoản dev bên dưới; trang chủ gọi
`GET /api/v1/ping` và hiển thị **pong**, phiên bản API, thời gian máy chủ.

> Ở môi trường Development, API tự chạy migration khi khởi động (`Database:MigrateOnStartup = true`)
> và nạp dữ liệu mẫu + tài khoản dev (`Database:SeedSampleData = true`).

## Tài khoản dev (CHỈ môi trường Development)

Mật khẩu chung: **`Upm@123456`**

| Tên đăng nhập | Vai trò | Phạm vi dữ liệu |
|---|---|---|
| `admin` | Quản trị hệ thống | Toàn hệ thống |
| `pdt.hn` | Phòng Đào tạo | Cơ sở HN |
| `pdt.hcm` | Phòng Đào tạo | Cơ sở HCM (UTC2) |
| `pdt.2coso` | Phòng Đào tạo | HN + HCM (chuyển cơ sở trên header) |
| `ldk.ct.hn` | Lãnh đạo Khoa | Khoa Công trình – HN |
| `tbm.cau.hn` | Trưởng Bộ môn (gắn GV001) | Bộ môn Cầu hầm – HN |
| `giaovu.ct.hn` | Giáo vụ | Khoa Công trình – HN |
| `gv001` | Giảng viên (GV001) | Khoa Công trình – HN |
| `gv003` | Giảng viên (GV003) | Khoa Kinh tế – HN |
| `gv004` | Giảng viên (GV004) | Khoa Công trình – HCM |
| `sv.65hn001` | Sinh viên (65HN001) | Bản thân |
| `thuvien.hn` | Thư viện | Cơ sở HN |

Gọi API từ Swagger: `POST /api/v1/auth/login` → copy `accessToken` → nút **Authorize**.

Dashboard Hangfire (`/hangfire`): chỉ vai trò **QuanTri**. Mở `http://localhost:5080/hangfire?access_token=<accessToken của admin>`
một lần; token được chuyển vào cookie HttpOnly cho các trang sau. Ngoài Development dashboard tắt, bật bằng
`Hangfire:DashboardEnabled = true`.

Đăng nhập sai 5 lần / 15 phút → tạm khóa tài khoản đó **trên IP đó** (429); giới hạn chung 300 lần đăng nhập/IP/phút
(SV dùng chung IP NAT). Tham số ở mục `XacThuc` của appsettings. Sau reverse proxy, khai báo IP proxy ở
`ForwardedHeaders:KnownProxies` / `KnownNetworks` để lấy đúng IP người dùng.
Khóa ký JWT dev nằm trong `appsettings.Development.json`; môi trường khác BẮT BUỘC đặt `Jwt:SigningKey`
(≥ 32 byte) qua user-secrets / biến môi trường `Jwt__SigningKey`, nếu không API sẽ không khởi động.

## Lệnh thường dùng

| Lệnh | Ý nghĩa |
|---|---|
| `docker compose up -d` / `docker compose down` | Bật / tắt SQL Server + Mailpit (dữ liệu giữ trong volume `upm-sqlserver-data`) |
| `dotnet build` | Build toàn solution `Upm.sln` |
| `dotnet test` | Chạy unit test + integration test |
| `dotnet ef migrations add <Ten> -p src/Upm.Infrastructure -s src/Upm.Api -o Persistence/Migrations` | Tạo migration |
| `dotnet ef database update -p src/Upm.Infrastructure -s src/Upm.Api` | Áp dụng migration thủ công |
| `cd web && npm run gen:api` | Tải `swagger.json` từ API đang chạy rồi sinh service/model TS vào `src/app/api` |
| `cd web && npm run build` / `npm run lint` / `npm test` | Build / lint / unit test frontend |
| `cd web && npm run format` | Format code frontend bằng Prettier |

## Cấu trúc thư mục

```
src/
  Upm.Domain           entity, luật nghiệp vụ thuần (Rules/, Workflow/)
  Upm.Application      use case, DTO, interface (IClock, …)
  Upm.Infrastructure   EF Core (UpmDbContext, migration), Hangfire, adapter ngoài
  Upm.Api              controller, ProblemDetails, Swagger, Serilog
tests/
  Upm.Domain.Tests           unit test luật & workflow
  Upm.Api.IntegrationTests   test API với SQL Server thật (Testcontainers)
web/                   Angular: src/app/{core,shared,features,api}
```

Tham chiếu một chiều: `Api → Infrastructure → Application → Domain`.

## Cấu hình

- Connection string: `ConnectionStrings:Upm` trong `src/Upm.Api/appsettings.Development.json`
  (mặc định khớp `docker-compose.yml`). Có thể ghi đè bằng user-secrets:
  `dotnet user-secrets set "ConnectionStrings:Upm" "<chuỗi kết nối>" --project src/Upm.Api`.
- Log: console + file `logs/upm-<ngày>.log` (thư mục chạy API), cấu hình ở mục `Serilog` của appsettings.
- CORS: `Cors:AllowedOrigins` (mặc định `http://localhost:4200`).
- Địa chỉ API cho `gen:api`: biến môi trường `UPM_API_URL` (mặc định `http://localhost:5080`).

## Lỗi API

Mọi lỗi trả về `application/problem+json` (RFC 9457) kèm `code` nghiệp vụ và `traceId`, ví dụ:

```json
{ "status": 404, "title": "Không tìm thấy tài nguyên yêu cầu.", "code": "UPM_NOT_FOUND", "traceId": "00-…" }
```

Frontend có interceptor đọc ProblemDetails và hiển thị thông báo tiếng Việt bằng `nz-message`.

## Xử lý sự cố

- **`npm install` báo `Cannot read properties of null (reading 'edgesOut')`**: lỗi của npm 10 khi dựng lại
  cây phụ thuộc. Dùng `npm ci` (cài theo `package-lock.json`) hoặc `npx npm@11 install <gói>` khi thêm gói mới.
- **Integration test lỗi Docker**: kiểm tra Docker Desktop đang chạy (`docker info`).
- **Port 1433 đã bị chiếm** (SQL Server cài sẵn trên máy): đặt `MSSQL_PORT=14330` trong `.env`
  và sửa connection string tương ứng.
