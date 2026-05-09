using Zzapcho.Launcher.Core.Sync;

namespace Zzapcho.Launcher.Core.Services;

public interface IClientIntegrityState
{
    event EventHandler<ClientIntegritySnapshot>? Changed;

    ClientIntegritySnapshot Current { get; }

    void Set(ClientIntegritySnapshot snapshot);
}
