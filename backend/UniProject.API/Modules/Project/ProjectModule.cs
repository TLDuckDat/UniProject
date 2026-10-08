using Microsoft.Extensions.DependencyInjection;

namespace UniProject.API.Modules.Project
{
    public static class ProjectModule
    {
        public static IServiceCollection AddProjectModule(this IServiceCollection services)
        {
            services.AddScoped<ProjectAccess>();
            services.AddScoped<IProjectService, ProjectService>();
            return services;
        }
    }
}
