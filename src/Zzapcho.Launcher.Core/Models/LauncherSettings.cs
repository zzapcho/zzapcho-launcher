namespace Zzapcho.Launcher.Core.Models;

public sealed class LauncherSettings
{
    public int RamMinMb { get; set; } = 1024;

    public int RamMaxMb { get; set; } = 4096;

    public bool AutoUpdate { get; set; } = true;

    public bool CrashReportConsent { get; set; }

    public string? LastSelectedAccountId { get; set; }

    public string Theme { get; set; } = "dark";

    public string ManifestUrl { get; set; } = ProductConstants.DefaultManifestUrl;

    public string LauncherUpdateSourceUrl { get; set; } = ProductConstants.DefaultLauncherUpdateSourceUrl;

    public static LauncherSettings CreateDefault() => new();

    public void Normalize()
    {
        if (RamMinMb < 512)
        {
            RamMinMb = 512;
        }

        if (RamMaxMb < RamMinMb)
        {
            RamMaxMb = RamMinMb;
        }

        if (RamMaxMb < 1024)
        {
            RamMaxMb = 1024;
        }

        if (string.IsNullOrWhiteSpace(Theme))
        {
            Theme = "dark";
        }

        if (string.IsNullOrWhiteSpace(ManifestUrl))
        {
            ManifestUrl = ProductConstants.DefaultManifestUrl;
        }

        if (string.IsNullOrWhiteSpace(LauncherUpdateSourceUrl))
        {
            LauncherUpdateSourceUrl = ProductConstants.DefaultLauncherUpdateSourceUrl;
        }
    }
}
