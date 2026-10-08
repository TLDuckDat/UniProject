using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UniProject.API.Common;
using UniProject.API.Data;
using UniProject.API.Modules.Academic;
using UniProject.API.Modules.Defense;
using UniProject.API.Modules.Grading;
using UniProject.API.Modules.Identity;
using UniProject.API.Modules.Notification;
using UniProject.API.Modules.Project;
using UniProject.API.Modules.ProjectPeriod;
using UniProject.API.Modules.Registration;
using UniProject.API.Modules.Topic;
using UniProject.Share.Application.Interfaces;
using UniProject.Share.Domain;

// Nạp .env trước CreateBuilder để Configuration đọc được
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Database
var connectionString = config.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Thiếu ConnectionStrings__DefaultConnection trong file .env.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<CurrentActor>();
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();

// Modules
builder.Services.AddIdentityModule();
builder.Services.AddAcademicModule();
builder.Services.AddProjectPeriodModule(config);
builder.Services.AddTopicModule();
builder.Services.AddRegistrationModule();
builder.Services.AddProjectModule();
builder.Services.AddDefenseModule();
builder.Services.AddGradingModule();
builder.Services.AddNotificationModule();

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)); // không escape tiếng Việt

// Lỗi model binding trả về cùng dạng Result
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());
        return new BadRequestObjectResult(Result.Fail("Dữ liệu không hợp lệ.", errors));
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(); // UseExceptionHandler() bắt buộc phải có, dù response do GlobalExceptionHandler tự ghi
builder.Services.AddSwaggerDocumentation();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("Database");

// JWT, Issuer/Audience/Key phải khớp với JwtTokenGenerator
var jwtKey = config["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Thiếu Jwt__Key trong file .env (tối thiểu 32 ký tự).");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"] ?? "QLDoAn",
            ValidAudience = config["Jwt:Audience"] ?? "QLDoAn",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
        // Tài khoản bị khóa hoặc đổi vai trò thì token cũ hết hiệu lực ngay
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal!;
                var role = principal.FindFirst(ClaimTypes.Role)?.Value;
                if (!int.TryParse(principal.FindFirst("UserId")?.Value, out var accountId))
                {
                    context.Fail("Token không chứa UserId.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var stillValid = await db.TaiKhoans.AnyAsync(u =>
                    u.Id == accountId && !u.DaKhoa && u.VaiTro.TenVaiTro == role);
                if (!stillValid)
                    context.Fail("Tài khoản đã bị khóa hoặc thay đổi quyền.");
            }
        };
    });

// Mặc định mọi endpoint đều cần đăng nhập, endpoint công khai thì gắn [AllowAnonymous]
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Giới hạn số lần gọi /auth/login mỗi phút cho mỗi IP (mặc định 10)
var loginPerMinute = config.GetValue("RateLimit:LoginPerMinute", 10);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = loginPerMinute, Window = TimeSpan.FromMinutes(1) }));
});

// Frontend HTML/CSS/JS chạy riêng (mặc định Live Server cổng 5500), thêm origin khác qua Cors__AllowedOrigins__0, __1...
var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5500", "http://127.0.0.1:5500"];
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

// Migration + seed. Không kết nối được SQL Server thì dừng app
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        await DbInitializer.InitializeAsync(
            services.GetRequiredService<AppDbContext>(),
            services.GetRequiredService<IPasswordHasher>(),
            services.GetRequiredService<ProjectTypePolicyResolver>(),
            services.GetRequiredService<IProjectGradeCalculator>(),
            app.Environment.ContentRootPath,
            logger,
            seedSampleData: config.GetValue<bool>("SeedSampleData"));
    }
    catch (Exception ex) when (DatabaseErrors.IsConnectionFailure(ex))
    {
        var db = new SqlConnectionStringBuilder(connectionString);
        logger.LogCritical(ex, "Không kết nối được SQL Server (Server={Server}, Database={Database}). Kiểm tra lại SQL Server và file .env.",
            db.DataSource, db.InitialCatalog);
        throw;
    }
}

app.Run();
