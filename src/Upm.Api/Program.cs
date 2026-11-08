using System.Reflection;
using Hangfire;
using Microsoft.OpenApi;
using Serilog;
using Upm.Api.Errors;
using Upm.Api.OpenApi;
using Upm.Application;
using Upm.Infrastructure;
using Upm.Infrastructure.Persistence;

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

// ---------- Web API ----------
builder.Services.AddControllers();
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

    var xmlFile =Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlFile))
    {
        options.IncludeXmlComments(xmlFile);
    }
});

var app = builder.Build();

// Migration phải chạy trước khi host khởi động Hangfire server (Hangfire cần DB đã tồn tại).
await app.Services.MigrateDatabaseIfConfiguredAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();
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

app.MapHealthChecks("/health");
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    // Dashboard Hangfire chỉ mở ở dev (mặc định chỉ cho truy cập từ localhost). Phân quyền làm ở Phần 3.
    app.MapHangfireDashboard("/hangfire");
}

await app.RunAsync();

/// <summary>Khai báo để WebApplicationFactory trong integration test tham chiếu được.</summary>
public partial class Program;
