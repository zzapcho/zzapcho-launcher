using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Manifest;
using Zzapcho.Launcher.Infrastructure.Manifest;

namespace Zzapcho.Launcher.Infrastructure.Sync;

public sealed class QuarantineService
{
    private readonly LauncherPaths _paths;

    public QuarantineService(LauncherPaths paths)
    {
        _paths = paths;
    }

    public int QuarantineUnknownFiles(LauncherManifest manifest)
    {
        if (!manifest.Sync.QuarantineUnknownFiles)
        {
            return 0;
        }

        var allowed = manifest.Files
            .Select(file => ManifestValidator.NormalizeManifestPath(file.Path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var count = 0;
        foreach (var protectedDirectory in manifest.Sync.ProtectedDirectories)
        {
            if (!ManifestValidator.IsSafeRelativePath(protectedDirectory))
            {
                continue;
            }

            var absoluteDirectory = Path.Combine(_paths.InstanceRoot, protectedDirectory);
            if (!Directory.Exists(absoluteDirectory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(absoluteDirectory, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(_paths.InstanceRoot, file).Replace('\\', '/');
                if (allowed.Contains(relative))
                {
                    continue;
                }

                var quarantinePath = Path.Combine(
                    _paths.QuarantineRoot,
                    DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss"),
                    relative.Replace('/', Path.DirectorySeparatorChar));

                Directory.CreateDirectory(Path.GetDirectoryName(quarantinePath)!);
                File.Move(file, quarantinePath, overwrite: true);
                count++;
            }
        }

        return count;
    }
}
