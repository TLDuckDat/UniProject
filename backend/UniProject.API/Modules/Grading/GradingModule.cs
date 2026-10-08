using Microsoft.Extensions.DependencyInjection;

namespace UniProject.API.Modules.Grading
{
    public static class GradingModule
    {
        public static IServiceCollection AddGradingModule(this IServiceCollection services)
        {
            services.AddScoped<IProjectGradeCalculator, ProjectGradeCalculator>();
            services.AddScoped<IGradingService, GradingService>();
            return services;
        }
    }
}
