using System.Security.Cryptography;
using System.Text;
using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.Infrastructure.Manifest;

public sealed class ManifestHashService : IManifestHashService
{
    public string ComputeHash(string manifestJson)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(manifestJson));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
