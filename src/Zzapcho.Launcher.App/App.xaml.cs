using System.IO;
using System.Net.Http;
using System.Windows;
using Velopack;
using Zzapcho.Launcher.App.ViewModels;
using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Infrastructure.Logging;
using Zzapcho.Launcher.Infrastructure.Manifest;
using Zzapcho.Launcher.Infrastructure.ServerStatus;
using Zzapcho.Launcher.Infrastructure.Settings;
using Zzapcho.Launcher.Infrastructure.Sync;
using Zzapcho.Launcher.Infrastructure.SystemShell;
using Zzapcho.Launcher.Infrastructure.Auth;
using Zzapcho.Launcher.Infrastructure.Crash;
using Zzapcho.Launcher.Infrastructure.Files;
using Zzapcho.Launcher.Infrastructure.Game;
using Zzapcho.Launcher.Infrastructure.Updates;

namespace Zzapcho.Launcher.App;

public partial class App : Application
{
    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    public MainViewModel CreateMainViewModel()
    {
        var paths = new LauncherPaths();
        paths.EnsureCreated();

        var logger = new FileAppLogger(paths);
        var settings = new JsonSettingsService(paths);
        var logReader = new LogReader(paths);
        var externalLauncher = new WindowsExternalLauncher();
        var serverStatus = new MinecraftPingStatusProvider();
        var httpClient = new HttpClient();
        var localManifest = Path.Combine(AppContext.BaseDirectory, "manifest", "launcher-manifest.sample.json");
        var manifestDownloader = new ManifestDownloader(httpClient, localManifest);
        var parser = new ManifestParser();
        var validator = new ManifestValidator();
        var signatureVerifier = new ManifestSignatureVerifier();
        var manifestHashService = new ManifestHashService();
        var hashVerifier = new FileHashVerifier();
        var fileDownloader = new FileDownloader(httpClient);
        var quarantineService = new QuarantineService(paths);
        var syncService = new FileSyncService(
            paths,
            settings,
            manifestDownloader,
            parser,
            validator,
            signatureVerifier,
            manifestHashService,
            hashVerifier,
            fileDownloader,
            quarantineService,
            logger);
        var integrityState = new ClientIntegrityState();
        var accountService = new DevAccountService();
        var fileInventoryService = new FileInventoryService(paths);
        var gamePreparationService = new NullGamePreparationService();
        var gameLaunchService = new NullGameLaunchService();
        var updateService = new VelopackUpdateService(settings, logger);
        var crashSupportService = new LocalCrashSupportService(paths);

        logger.Info("런처를 시작했습니다.");
        return new MainViewModel(
            paths,
            settings,
            logReader,
            logger,
            externalLauncher,
            serverStatus,
            syncService,
            integrityState,
            accountService,
            fileInventoryService,
            gamePreparationService,
            gameLaunchService,
            updateService,
            crashSupportService);
    }
}
