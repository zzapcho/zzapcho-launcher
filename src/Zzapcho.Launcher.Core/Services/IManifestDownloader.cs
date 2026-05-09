namespace Zzapcho.Launcher.Core.Services;

public interface IManifestDownloader
{
    Task<string> DownloadOrLoadAsync(string manifestUrl, CancellationToken cancellationToken = default);
}
