using Microsoft.OpenApi;

namespace UniProject.API.Common
{
    public static class SwaggerSetup
    {
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Hệ thống quản lý đồ án cơ sở & đồ án chuyên ngành API",
                    Version = "v1",
                    Description = "Backend API xây dựng theo kiến trúc Modular Monolith trên nền tảng .NET 10 & EF Core."
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Nhập JWT token (không cần gõ chữ 'Bearer').",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                // Swashbuckle 10 (Microsoft.OpenApi 2.x): tham chiếu scheme qua OpenApiSecuritySchemeReference
                c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });

            return services;
        }
    }
}
