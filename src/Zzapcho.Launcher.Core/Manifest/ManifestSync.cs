namespace Zzapcho.Launcher.Core.Manifest;

public sealed class ManifestSync
{
    public bool DeleteUnknownFiles { get; set; }

    public bool QuarantineUnknownFiles { get; set; } = true;

    public List<string> ProtectedDirectories { get; set; } = new()
    {
        "mods",
        "resourcepacks",
        "shaderpacks",
        "config"
    };
}
