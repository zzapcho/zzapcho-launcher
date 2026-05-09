using System.Security.Cryptography;

namespace Zzapcho.Launcher.Infrastructure.Sync;

public sealed class FileHashVerifier
{
    public async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task<bool> VerifyAsync(string filePath, string expectedSha256, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        if (expectedSha256.Equals("PUT_SHA256_HERE", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var actual = await ComputeSha256Async(filePath, cancellationToken);
        return actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase);
    }
}
