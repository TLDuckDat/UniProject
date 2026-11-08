using System.Reflection;
using Upm.Application.Abstractions;

namespace Upm.Application.SystemInfo;

public interface ISystemInfoService
{
    PingResponse Ping();
}

public sealed class SystemInfoService(IClock clock) : ISystemInfoService
{
    private static readonly string AppVersion = ResolveVersion();

    public PingResponse Ping() => new("pong", AppVersion, clock.Now);

    private static string ResolveVersion()
    {
        // Lấy version của assembly Application (cùng <Version> trong Directory.Build.props),
        // không dùng entry assembly vì khi chạy test entry assembly là test host.
        var assembly = typeof(SystemInfoService).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        // Bỏ phần "+<commit hash>" mà SDK tự gắn vào InformationalVersion.
        return informational?.Split('+')[0] ?? assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}
