using Microsoft.Extensions.DependencyInjection;

namespace UniProject.API.Modules.Notification
{
    public static class NotificationModule
    {
        public static IServiceCollection AddNotificationModule(this IServiceCollection services)
        {
            services.AddScoped<INotificationService, NotificationService>();
            return services;
        }
    }
}
