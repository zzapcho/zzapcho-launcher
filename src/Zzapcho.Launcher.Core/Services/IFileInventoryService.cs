using Zzapcho.Launcher.Core.Models;

namespace Zzapcho.Launcher.Core.Services;

public interface IFileInventoryService
{
    Task<IReadOnlyList<ManagedFileItem>> ListAsync(string category, CancellationToken cancellationToken = default);
}
