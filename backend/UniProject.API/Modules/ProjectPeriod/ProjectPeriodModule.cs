using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace UniProject.API.Modules.ProjectPeriod
{
    public static class ProjectPeriodModule
    {
        public static IServiceCollection AddProjectPeriodModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Tham số từng loại đồ án đọc từ cấu hình; sai thì dừng ứng dụng ngay khi khởi động
            services.AddOptions<ProjectTypesOptions>()
                .Bind(configuration.GetSection(ProjectTypesOptions.SectionName))
                .ValidateOnStart();
            services.AddSingleton<IValidateOptions<ProjectTypesOptions>, ProjectTypesOptionsValidator>();

            // Strategy + Factory cho từng loại đồ án
            services.AddScoped<IProjectTypePolicy, DoAnCoSoPolicy>();
            services.AddScoped<IProjectTypePolicy, DoAnChuyenNganhPolicy>();
            services.AddScoped<IProjectTypePolicy, DoAnMonHocPolicy>();
            services.AddScoped<IProjectTypePolicy, DoAnTotNghiepPolicy>();
            services.AddScoped<ProjectTypePolicyResolver>();
            services.AddScoped<IProjectPeriodService, ProjectPeriodService>();
            return services;
        }
    }
}
