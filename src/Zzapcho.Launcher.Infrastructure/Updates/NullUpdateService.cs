using System.Reflection;
using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.Infrastructure.Updates;

public sealed class NullUpdateService : IUpdateService
{
    public Task<LauncherUpdateState> CheckAsync(CancellationToken cancellationToken = default)
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";
        return Task.FromResult(new LauncherUpdateState(version, version, false, false, "최신 버전입니다"));
    }
}
