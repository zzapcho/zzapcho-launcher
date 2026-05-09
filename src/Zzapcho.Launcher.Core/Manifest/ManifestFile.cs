namespace Zzapcho.Launcher.Core.Manifest;

public sealed class ManifestFile
{
    public string Path { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;

    public long Size { get; set; }

    public bool Required { get; set; } = true;
}
