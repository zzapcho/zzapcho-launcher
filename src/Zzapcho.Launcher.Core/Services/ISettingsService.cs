using Zzapcho.Launcher.Core.Models;

namespace Zzapcho.Launcher.Core.Services;

public interface ISettingsService
{
    Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken = default);

    Task<LauncherSettings> ResetAsync(CancellationToken cancellationToken = default);
}
