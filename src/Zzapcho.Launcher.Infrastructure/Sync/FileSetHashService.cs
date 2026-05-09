using System.Security.Cryptography;
using System.Text;
using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Manifest;
using Zzapcho.Launcher.Infrastructure.Manifest;

namespace Zzapcho.Launcher.Infrastructure.Sync;

public sealed class FileSetHashService
{
    private readonly LauncherPaths _paths;
    private readonly FileHashVerifier _hashVerifier;

    public FileSetHashService(LauncherPaths paths, FileHashVerifier hashVerifier)
    {
        _paths = paths;
        _hashVerifier = hashVerifier;
    }

    public async Task<string> ComputeAsync(LauncherManifest manifest, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        foreach (var file in manifest.Files.OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase))
        {
            var absolute = Path.Combine(_paths.InstanceRoot, ManifestValidator.NormalizeManifestPath(file.Path));
            if (!File.Exists(absolute))
            {
                continue;
            }

            builder.Append(ManifestValidator.NormalizeManifestPath(file.Path));
            builder.Append(':');
            builder.Append(await _hashVerifier.ComputeSha256Async(absolute, cancellationToken));
            builder.AppendLine();
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
    }
}
