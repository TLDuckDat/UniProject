using Microsoft.Extensions.DependencyInjection;

namespace UniProject.API.Modules.Defense
{
    public static class DefenseModule
    {
        public static IServiceCollection AddDefenseModule(this IServiceCollection services)
        {
            services.AddScoped<IDefenseService, DefenseService>();
            return services;
        }
    }
}
