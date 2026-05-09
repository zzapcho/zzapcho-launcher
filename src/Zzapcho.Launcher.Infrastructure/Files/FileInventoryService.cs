using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Models;
using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.Infrastructure.Files;

public sealed class FileInventoryService : IFileInventoryService
{
    private readonly LauncherPaths _paths;

    public FileInventoryService(LauncherPaths paths)
    {
        _paths = paths;
    }

    public Task<IReadOnlyList<ManagedFileItem>> ListAsync(string category, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(_paths.InstanceRoot, category);
        if (!Directory.Exists(directory))
        {
            return Task.FromResult<IReadOnlyList<ManagedFileItem>>(Array.Empty<ManagedFileItem>());
        }

        var files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Select(path => new ManagedFileItem(
                Path.GetFileName(path),
                Path.GetRelativePath(_paths.InstanceRoot, path).Replace('\\', '/'),
                true,
                category))
            .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult<IReadOnlyList<ManagedFileItem>>(files);
    }
}
