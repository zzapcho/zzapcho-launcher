using System.IO.Compression;
using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.Infrastructure.Crash;

public sealed class LocalCrashSupportService : ICrashSupportService
{
    private readonly LauncherPaths _paths;

    public LocalCrashSupportService(LauncherPaths paths)
    {
        _paths = paths;
    }

    public Task<string> CreateSupportZipAsync(CancellationToken cancellationToken = default)
    {
        _paths.EnsureCreated();
        var zipPath = Path.Combine(_paths.DataRoot, $"support-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.zip");
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        AddIfExists(archive, _paths.LauncherLogFile, "logs/launcher.log");
        AddIfExists(archive, _paths.GameLogFile, "logs/game.log");
        AddIfExists(archive, _paths.CrashLogFile, "logs/crash.log");
        AddIfExists(archive, _paths.SettingsFile, "settings.json");
        return Task.FromResult(zipPath);
    }

    private static void AddIfExists(ZipArchive archive, string path, string entryName)
    {
        if (File.Exists(path))
        {
            archive.CreateEntryFromFile(path, entryName);
        }
    }
}
