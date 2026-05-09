using Zzapcho.Launcher.Core.Sync;

namespace Zzapcho.Launcher.Core.Services;

public interface IFileSyncService
{
    Task<FileSyncReport> CheckAsync(FileSyncOptions options, CancellationToken cancellationToken = default);
}
