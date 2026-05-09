namespace Zzapcho.Launcher.Core.Manifest;

public sealed class LauncherManifest
{
    public int SchemaVersion { get; set; }

    public string ProfileId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string ManifestVersion { get; set; } = string.Empty;

    public ManifestServer Server { get; set; } = new();

    public ManifestMinecraft Minecraft { get; set; } = new();

    public ManifestLauncher Launcher { get; set; } = new();

    public ManifestSync Sync { get; set; } = new();

    public List<ManifestFile> Files { get; set; } = new();

    public string Signature { get; set; } = string.Empty;
}
