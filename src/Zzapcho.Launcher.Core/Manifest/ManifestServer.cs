namespace Zzapcho.Launcher.Core.Manifest;

public sealed class ManifestServer
{
    public string Host { get; set; } = ProductConstants.ServerHost;

    public int Port { get; set; } = ProductConstants.ServerPort;
}
