using System.Reflection;
using Velopack;
using Velopack.Exceptions;
using Velopack.Sources;
using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.Infrastructure.Updates;

public sealed class VelopackUpdateService : IUpdateService
{
    private readonly ISettingsService _settingsService;
    private readonly IAppLogger _logger;

    public VelopackUpdateService(ISettingsService settingsService, IAppLogger logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<LauncherUpdateState> CheckAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = GetCurrentVersion();
        var settings = await _settingsService.LoadAsync(cancellationToken);

        if (!settings.AutoUpdate)
        {
            return new LauncherUpdateState(currentVersion, currentVersion, false, false, "자동 업데이트가 꺼져 있습니다.");
        }

        try
        {
            var source = new GithubSource(settings.LauncherUpdateSourceUrl, accessToken: null, prerelease: true);
            var manager = new UpdateManager(source);

            if (!manager.IsInstalled)
            {
                return new LauncherUpdateState(currentVersion, currentVersion, false, false, "설치된 배포판에서만 런처 업데이트를 확인합니다.");
            }

            var update = await manager.CheckForUpdatesAsync();
            if (update is null)
            {
                return new LauncherUpdateState(currentVersion, currentVersion, false, false, "최신 버전입니다.");
            }

            var latestVersion = update.TargetFullRelease.Version.ToString();
            return new LauncherUpdateState(currentVersion, latestVersion, true, false, $"새 런처 버전 {latestVersion}을 사용할 수 있습니다.");
        }
        catch (NotInstalledException)
        {
            return new LauncherUpdateState(currentVersion, currentVersion, false, false, "개발 실행에서는 런처 업데이트를 건너뜁니다.");
        }
        catch (Exception ex)
        {
            _logger.Error("런처 업데이트 확인 중 오류가 발생했습니다.", ex);
            return new LauncherUpdateState(currentVersion, currentVersion, false, false, "런처 업데이트를 확인하지 못했습니다.");
        }
    }

    private static string GetCurrentVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        var informational = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            return informational;
        }

        return assembly?.GetName().Version?.ToString(3) ?? "1.0.0";
    }
}
