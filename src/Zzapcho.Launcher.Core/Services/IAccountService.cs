using Zzapcho.Launcher.Core.Models;

namespace Zzapcho.Launcher.Core.Services;

public interface IAccountService
{
    Task<AccountState> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<AccountState> LoginAsync(CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);
}
