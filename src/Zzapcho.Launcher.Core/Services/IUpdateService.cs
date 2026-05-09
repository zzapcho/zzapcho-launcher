namespace Zzapcho.Launcher.Core.Services;

public interface IUpdateService
{
    Task<LauncherUpdateState> CheckAsync(CancellationToken cancellationToken = default);
}

public sealed record LauncherUpdateState(string CurrentVersion, string LatestVersion, bool UpdateAvailable, bool Mandatory, string Message);
