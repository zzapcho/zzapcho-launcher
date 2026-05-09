using Zzapcho.Launcher.Core.Models;

namespace Zzapcho.Launcher.Core.Services;

public interface IServerStatusProvider
{
    Task<ServerStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}
