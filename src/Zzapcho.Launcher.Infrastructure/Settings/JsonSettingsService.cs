using System.Text.Json;
using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Models;
using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.Infrastructure.Settings;

public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly LauncherPaths _paths;

    public JsonSettingsService(LauncherPaths paths)
    {
        _paths = paths;
    }

    public async Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        _paths.EnsureCreated();

        if (!File.Exists(_paths.SettingsFile))
        {
            return await ResetAsync(cancellationToken);
        }

        await using var stream = File.OpenRead(_paths.SettingsFile);
        var settings = await JsonSerializer.DeserializeAsync<LauncherSettings>(stream, JsonOptions, cancellationToken)
            ?? LauncherSettings.CreateDefault();
        settings.Normalize();
        await SaveAsync(settings, cancellationToken);
        return settings;
    }

    public async Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken = default)
    {
        _paths.EnsureCreated();
        settings.Normalize();
        await using var stream = File.Create(_paths.SettingsFile);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
    }

    public async Task<LauncherSettings> ResetAsync(CancellationToken cancellationToken = default)
    {
        var settings = LauncherSettings.CreateDefault();
        await SaveAsync(settings, cancellationToken);
        return settings;
    }
}
