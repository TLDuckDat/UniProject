using Microsoft.Extensions.DependencyInjection;
using Upm.Application.SystemInfo;
using Upm.Application.Workflow;

namespace Upm.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISystemInfoService, SystemInfoService>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        return services;
    }
}
