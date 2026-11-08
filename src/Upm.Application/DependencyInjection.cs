using Microsoft.Extensions.DependencyInjection;
using Upm.Application.SystemInfo;

namespace Upm.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISystemInfoService, SystemInfoService>();
        return services;
    }
}
