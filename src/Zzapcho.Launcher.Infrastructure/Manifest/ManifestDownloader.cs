using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.Infrastructure.Manifest;

public sealed class ManifestDownloader : IManifestDownloader
{
    private readonly HttpClient _httpClient;
    private readonly string _localSampleManifestPath;

    public ManifestDownloader(HttpClient httpClient, string localSampleManifestPath)
    {
        _httpClient = httpClient;
        _localSampleManifestPath = localSampleManifestPath;
    }

    public async Task<string> DownloadOrLoadAsync(string manifestUrl, CancellationToken cancellationToken = default)
    {
        if (IsPlaceholderUrl(manifestUrl))
        {
            return await File.ReadAllTextAsync(_localSampleManifestPath, cancellationToken);
        }

        using var response = await _httpClient.GetAsync(manifestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static bool IsPlaceholderUrl(string manifestUrl)
    {
        return string.IsNullOrWhiteSpace(manifestUrl) ||
               manifestUrl.Equals(ProductConstants.DefaultManifestUrl, StringComparison.OrdinalIgnoreCase) ||
               manifestUrl.Contains("OWNER/REPO", StringComparison.OrdinalIgnoreCase);
    }
}
