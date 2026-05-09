using Zzapcho.Launcher.Core.Models;
using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.Infrastructure.Auth;

public sealed class DevAccountService : IAccountService
{
    private AccountState _state = AccountState.SignedOut;

    public Task<AccountState> GetCurrentAsync(CancellationToken cancellationToken = default) => Task.FromResult(_state);

    public Task<AccountState> LoginAsync(CancellationToken cancellationToken = default)
    {
        _state = new AccountState(true, "zzapcho", "00000000-0000-0000-0000-000000000000");
        return Task.FromResult(_state);
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        _state = AccountState.SignedOut;
        return Task.CompletedTask;
    }
}
