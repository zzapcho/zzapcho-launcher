namespace Zzapcho.Launcher.Core.Sync;

public sealed class FileSyncOptions
{
    public bool Repair { get; init; }

    public bool AllowDevelopmentManifestPlaceholder { get; init; } = true;
}
