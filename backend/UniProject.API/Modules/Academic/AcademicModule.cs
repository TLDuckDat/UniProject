using Microsoft.Extensions.DependencyInjection;

namespace UniProject.API.Modules.Academic
{
    public static class AcademicModule
    {
        public static IServiceCollection AddAcademicModule(this IServiceCollection services)
        {
            services.AddScoped<AcademicService>();
            services.AddScoped<IAcademicService>(sp => sp.GetRequiredService<AcademicService>());
            services.AddScoped<IFacultyService>(sp => sp.GetRequiredService<AcademicService>());
            services.AddScoped<IDepartmentService>(sp => sp.GetRequiredService<AcademicService>());
            services.AddScoped<IMajorService>(sp => sp.GetRequiredService<AcademicService>());
            services.AddScoped<IClassService>(sp => sp.GetRequiredService<AcademicService>());
            services.AddScoped<IAcademicYearService>(sp => sp.GetRequiredService<AcademicService>());
            services.AddScoped<ILecturerService>(sp => sp.GetRequiredService<AcademicService>());
            services.AddScoped<IStudentService>(sp => sp.GetRequiredService<AcademicService>());
            return services;
        }
    }
}
