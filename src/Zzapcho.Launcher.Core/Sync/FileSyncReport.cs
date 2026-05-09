namespace Zzapcho.Launcher.Core.Sync;

public sealed class FileSyncReport
{
    public FileCheckStatus Status { get; init; }

    public string StatusText { get; init; } = "파일 확인 전";

    public string ManifestVersion { get; init; } = "-";

    public string ManifestHash { get; init; } = "-";

    public string MinecraftVersion { get; init; } = "-";

    public string LoaderVersion { get; init; } = "-";

    public int FilesChecked { get; init; }

    public int FilesDownloaded { get; init; }

    public int FilesMissing { get; init; }

    public int FilesInvalid { get; init; }

    public int FilesQuarantined { get; init; }

    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();

    public bool IsReady => Status == FileCheckStatus.Ready;
}
