using Zzapcho.Launcher.Core.Services;
using Zzapcho.Launcher.Core.Sync;

namespace Zzapcho.Launcher.Infrastructure.Sync;

public sealed class ClientIntegrityState : IClientIntegrityState
{
    private ClientIntegritySnapshot _current = ClientIntegritySnapshot.Initial;

    public event EventHandler<ClientIntegritySnapshot>? Changed;

    public ClientIntegritySnapshot Current => _current;

    public void Set(ClientIntegritySnapshot snapshot)
    {
        _current = snapshot;
        Changed?.Invoke(this, snapshot);
    }
}
