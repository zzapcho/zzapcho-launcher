namespace Zzapcho.Launcher.Core.Services;

public interface IManifestHashService
{
    string ComputeHash(string manifestJson);
}
