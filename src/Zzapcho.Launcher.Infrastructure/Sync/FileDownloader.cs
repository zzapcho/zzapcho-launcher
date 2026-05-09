namespace Zzapcho.Launcher.Infrastructure.Sync;

public sealed class FileDownloader
{
    private readonly HttpClient _httpClient;

    public FileDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task DownloadAtomicAsync(string url, string destinationPath, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        var tempPath = destinationPath + ".download";

        await using (var remote = await _httpClient.GetStreamAsync(url, cancellationToken))
        await using (var local = File.Create(tempPath))
        {
            await remote.CopyToAsync(local, cancellationToken);
        }

        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        File.Move(tempPath, destinationPath);
    }
}
