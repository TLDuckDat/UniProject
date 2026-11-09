using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Upm.Api.Authorization;
using Upm.Api.Controllers;
using Upm.Api.Errors;
using Upm.Api.OpenApi;
using Upm.Application;
using Upm.Infrastructure;
using Upm.Infrastructure.Identity;
using Upm.Infrastructure.Persistence;
using Upm.Infrastructure.TienDo;

const string WebCorsPolicy = "web";

var builder = WebApplication.CreateBuilder(args);

// ---------- Logging: Serilog (console + file, cấu hình trong appsettings "Serilog") ----------
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// ---------- Tầng Application / Infrastructure ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// ---------- Xác thực: JWT bearer (access token 15 phút) ----------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Đọc cấu hình lúc chạy (không lúc đăng ký) để test ghi đè được.
        var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        options.MapInboundClaims = false; // giữ nguyên "sub", "name"…
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = string.IsNullOrEmpty(jwt.SigningKey) ? null : TokenService.TaoKhoaKy(jwt.SigningKey),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = UpmClaims.TenDangNhap,
        };
        // Dashboard Hangfire: lấy token từ query (lần đầu) hoặc cookie upm_hf.
        options.Events = new JwtBearerEvents();
        HangfireDashboard.CauHinhJwt(options.Events);
    });

// ---------- IP thật sau reverse proxy (nginx/IIS ARR): chỉ tin X-Forwarded-* từ proxy đã khai báo ----------
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var ip in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    {
        options.KnownProxies.Add(IPAddress.Parse(ip));
    }
    foreach (var cidr in builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
    {
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(cidr));
    }
});

// ---------- Phân quyền: mặc định BẮT BUỘC đăng nhập; quyền chức năng qua [CoQuyen] ----------
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddSingleton<IAuthorizationPolicyProvider, QuyenPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, QuyenHandler>();

// ---------- Chống dò hàng loạt: giới hạn request đăng nhập / IP / phút (rộng – SV dùng chung IP NAT) ----------
// Chống dò mật khẩu TỪNG tài khoản: DangNhapSaiTracker khóa tạm cặp (tài khoản, IP) sau N lần sai.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(XacThucController.DangNhapRateLimit, ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("XacThuc:SoLanDangNhapMoiPhut", 300),
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

// ---------- Web API ----------
// Enum ra/vào JSON dạng chuỗi (TrangThaiSVTrongDot, LoaiNo…) – khớp cách lưu DB (CLAUDE.md §6) và để FE/OpenAPI đọc được.
// Vẫn nhận số khi đọc (AllowIntegerValues mặc định) nên client cũ không vỡ.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddUpmProblemDetails();

builder.Services.AddCors(options => options.AddPolicy(WebCorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UPM API – Quản lý Đồ án tốt nghiệp",
        Version = "v1",
        Description = "API hệ thống quản lý ĐATN UTC / UTC2.",
    });

    // Kiểu non-nullable của C# → "required" + không nullable trong OpenAPI (model TS chặt hơn).
    options.SupportNonNullableReferenceTypes();
    options.NonNullableReferenceTypesAsRequired();
    options.OperationFilter<JsonOnlyContentFilter>();

    // Nút "Authorize" trong Swagger UI: dán access token lấy từ /api/v1/auth/login.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Access token (JWT) từ POST /api/v1/auth/login.",
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
    });

    var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlFile))
    {
        options.IncludeXmlComments(xmlFile);
    }
});

var app = builder.Build();

// Migration phải chạy trước khi host khởi động Hangfire server (Hangfire cần DB đã tồn tại).
await app.Services.MigrateDatabaseIfConfiguredAsync();

app.UseForwardedHeaders(); // phải đứng đầu để mọi middleware sau thấy IP thật
app.UseExceptionHandler();
app.UseStatusCodePages(); // 401/403/404/429 rỗng → ProblemDetails tiếng Việt
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "UPM API v1");
        options.DocumentTitle = "UPM API";
    });
}

app.UseCors(WebCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

// UC-3a: nhắc việc tiến độ hằng ngày (Hangfire). Bỏ qua khi test – test gọi job trực tiếp, không phụ thuộc lịch.
if (!app.Environment.IsEnvironment("Testing"))
{
    // IRecurringJobManager (DI): RecurringJob tĩnh cần JobStorage.Current, không được đặt khi dùng DI.
    app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate<NhacViecHangfireJob>(
        NhacViecHangfireJob.IdLich,
        job => job.ChayAsync(CancellationToken.None),
        Cron.Daily(1),
        new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById(app.Configuration["Clock:TimeZone"] ?? "Asia/Ho_Chi_Minh") });
}

// Dashboard Hangfire: mặc định chỉ bật ở Development; môi trường khác bật bằng Hangfire:DashboardEnabled = true.
// Chỉ vai trò QuanTri (HangfireQuanTriFilter); AllowAnonymous để Hangfire tự trả 401/403 qua filter của nó.
if (app.Configuration.GetValue("Hangfire:DashboardEnabled", app.Environment.IsDevelopment()))
{
    app.MapHangfireDashboard(HangfireDashboard.Path, new DashboardOptions
    {
        Authorization = [],
        AsyncAuthorization = [new HangfireQuanTriFilter()],
        DashboardTitle = "UPM – Job nền",
    }).AllowAnonymous();
}

await app.RunAsync();

/// <summary>Khai báo để WebApplicationFactory trong integration test tham chiếu được.</summary>
public partial class Program;
