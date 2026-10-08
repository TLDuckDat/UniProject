using Microsoft.Extensions.DependencyInjection;

namespace UniProject.API.Modules.Registration
{
    public static class RegistrationModule
    {
        public static IServiceCollection AddRegistrationModule(this IServiceCollection services)
        {
            services.AddScoped<IRegistrationService, RegistrationService>();
            services.AddScoped<IRegistrationChangeService, RegistrationChangeService>();
            return services;
        }
    }
}
