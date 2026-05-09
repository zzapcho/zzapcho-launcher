using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Manifest;
using Zzapcho.Launcher.Core.Models;
using Zzapcho.Launcher.Infrastructure.Logging;
using Zzapcho.Launcher.Infrastructure.Manifest;
using Zzapcho.Launcher.Infrastructure.ServerStatus;
using Zzapcho.Launcher.Infrastructure.Settings;
using Zzapcho.Launcher.Infrastructure.Sync;
using Zzapcho.Launcher.Infrastructure.Auth;
using Zzapcho.Launcher.Infrastructure.Crash;

var tests = new LauncherTestSuite();
await tests.RunAsync();

internal sealed class LauncherTestSuite
{
    private int _passed;

    public async Task RunAsync()
    {
        await RunAsync("Settings persistence creates defaults", SettingsPersistenceCreatesDefaultsAsync);
        await RunAsync("Settings normalize RAM values", SettingsNormalizeRamValuesAsync);
        await RunAsync("Server ping JSON parser reads sample players", ServerPingParserReadsSamplePlayersAsync);
        await RunAsync("Logger writes launcher log", LoggerWritesLauncherLogAsync);
        await RunAsync("Manifest parser reads versions", ManifestParserReadsVersionsAsync);
        await RunAsync("Manifest validator rejects path traversal", ManifestValidatorRejectsPathTraversalAsync);
        await RunAsync("SHA-256 verifier detects valid file", Sha256VerifierDetectsValidFileAsync);
        await RunAsync("Quarantine service moves unknown files", QuarantineServiceMovesUnknownFilesAsync);
        await RunAsync("Dev account login state works", DevAccountLoginStateWorksAsync);
        await RunAsync("Support ZIP is created", SupportZipIsCreatedAsync);

        Console.WriteLine($"{_passed} tests passed.");
    }

    private async Task RunAsync(string name, Func<Task> test)
    {
        try
        {
            await test();
            _passed++;
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL {name}");
            Console.Error.WriteLine(ex);
            Environment.ExitCode = 1;
        }
    }

    private static async Task SettingsPersistenceCreatesDefaultsAsync()
    {
        var temp = CreateTempRoot();
        var paths = new LauncherPaths(temp);
        var service = new JsonSettingsService(paths);

        var settings = await service.LoadAsync();

        Assert.Equal(1024, settings.RamMinMb);
        Assert.Equal(4096, settings.RamMaxMb);
        Assert.True(settings.AutoUpdate);
        Assert.Equal(ProductConstants.DefaultLauncherUpdateSourceUrl, settings.LauncherUpdateSourceUrl);
        Assert.True(File.Exists(paths.SettingsFile));
    }

    private static Task SettingsNormalizeRamValuesAsync()
    {
        var settings = new LauncherSettings
        {
            RamMinMb = 128,
            RamMaxMb = 256,
            Theme = string.Empty
        };

        settings.Normalize();

        Assert.Equal(512, settings.RamMinMb);
        Assert.Equal(1024, settings.RamMaxMb);
        Assert.Equal("dark", settings.Theme);
        return Task.CompletedTask;
    }

    private static Task ServerPingParserReadsSamplePlayersAsync()
    {
        const string json = """
        {
          "version": { "name": "Paper 1.21.4", "protocol": 767 },
          "players": {
            "max": 100,
            "online": 2,
            "sample": [
              { "name": "zzapcho", "id": "uuid-1" },
              { "name": "player123", "id": "uuid-2" }
            ]
          },
          "description": { "text": "잡초 약탈서버" }
        }
        """;

        var status = MinecraftPingStatusProvider.ParseStatusJson(json, 42);

        Assert.Equal(ServerStatusState.Online, status.State);
        Assert.Equal(2, status.CurrentPlayers);
        Assert.Equal(100, status.MaxPlayers);
        Assert.Equal("Paper 1.21.4", status.Version);
        Assert.Equal("잡초 약탈서버", status.Motd);
        Assert.Equal(42L, status.LatencyMs);
        Assert.Equal("zzapcho", status.Players[0].Name);
        return Task.CompletedTask;
    }

    private static async Task LoggerWritesLauncherLogAsync()
    {
        var temp = CreateTempRoot();
        var paths = new LauncherPaths(temp);
        var logger = new FileAppLogger(paths);

        logger.Info("테스트 로그");
        var content = await File.ReadAllTextAsync(paths.LauncherLogFile);

        Assert.Contains("테스트 로그", content);
    }

    private static Task ManifestParserReadsVersionsAsync()
    {
        var json = """
        {
          "schemaVersion": 1,
          "profileId": "zzapcho-raid-main",
          "displayName": "잡초 약탈서버",
          "manifestVersion": "2026.05.09-001",
          "server": { "host": "online.zzapcho.kr", "port": 25565 },
          "minecraft": { "version": "1.21.4", "loader": "fabric", "loaderVersion": "0.16.9" },
          "launcher": { "minimumVersion": "1.0.0", "latestVersion": "1.0.0" },
          "sync": { "deleteUnknownFiles": false, "quarantineUnknownFiles": true, "protectedDirectories": ["mods"] },
          "files": [],
          "signature": "PUT_SIGNATURE_HERE"
        }
        """;

        var manifest = new ManifestParser().Parse(json);

        Assert.Equal("2026.05.09-001", manifest.ManifestVersion);
        Assert.Equal("1.21.4", manifest.Minecraft.Version);
        return Task.CompletedTask;
    }

    private static Task ManifestValidatorRejectsPathTraversalAsync()
    {
        var manifest = CreateValidManifest();
        manifest.Files.Add(new ManifestFile
        {
            Path = "../evil.exe",
            Url = "https://example.com/evil.exe",
            Sha256 = "PUT_SHA256_HERE",
            Size = 1,
            Required = true
        });

        var result = new ManifestValidator().Validate(manifest);

        Assert.False(result.IsValid);
        Assert.Contains("안전하지", string.Join('\n', result.Errors));
        return Task.CompletedTask;
    }

    private static async Task Sha256VerifierDetectsValidFileAsync()
    {
        var temp = CreateTempRoot();
        var file = Path.Combine(temp, "test.txt");
        await File.WriteAllTextAsync(file, "zzapcho");

        var verifier = new FileHashVerifier();
        var hash = await verifier.ComputeSha256Async(file);

        Assert.True(await verifier.VerifyAsync(file, hash));
    }

    private static async Task QuarantineServiceMovesUnknownFilesAsync()
    {
        var temp = CreateTempRoot();
        var paths = new LauncherPaths(temp);
        paths.EnsureCreated();
        var unknown = Path.Combine(paths.InstanceRoot, "mods", "unknown.jar");
        Directory.CreateDirectory(Path.GetDirectoryName(unknown)!);
        await File.WriteAllTextAsync(unknown, "unknown");

        var manifest = CreateValidManifest();
        manifest.Files.Clear();
        manifest.Sync.ProtectedDirectories = new List<string> { "mods" };

        var count = new QuarantineService(paths).QuarantineUnknownFiles(manifest);

        Assert.Equal(1, count);
        Assert.False(File.Exists(unknown));
        Assert.True(Directory.EnumerateFiles(paths.QuarantineRoot, "*", SearchOption.AllDirectories).Any());
    }

    private static async Task DevAccountLoginStateWorksAsync()
    {
        var account = new DevAccountService();
        Assert.False((await account.GetCurrentAsync()).IsLoggedIn);

        var loggedIn = await account.LoginAsync();
        Assert.True(loggedIn.IsLoggedIn);
        Assert.Equal("zzapcho", loggedIn.PlayerName);

        await account.LogoutAsync();
        Assert.False((await account.GetCurrentAsync()).IsLoggedIn);
    }

    private static async Task SupportZipIsCreatedAsync()
    {
        var temp = CreateTempRoot();
        var paths = new LauncherPaths(temp);
        paths.EnsureCreated();
        await File.WriteAllTextAsync(paths.LauncherLogFile, "launcher log");

        var zip = await new LocalCrashSupportService(paths).CreateSupportZipAsync();

        Assert.True(File.Exists(zip));
    }

    private static LauncherManifest CreateValidManifest() => new()
    {
        SchemaVersion = 1,
        ProfileId = ProductConstants.ProfileId,
        DisplayName = "잡초 약탈서버",
        ManifestVersion = "2026.05.09-001",
        Server = new ManifestServer
        {
            Host = ProductConstants.ServerHost,
            Port = ProductConstants.ServerPort
        },
        Minecraft = new ManifestMinecraft
        {
            Version = "1.21.4",
            Loader = "fabric",
            LoaderVersion = "0.16.9"
        },
        Launcher = new ManifestLauncher
        {
            MinimumVersion = "1.0.0",
            LatestVersion = "1.0.0"
        },
        Sync = new ManifestSync
        {
            ProtectedDirectories = new List<string> { "mods" }
        },
        Signature = "PUT_SIGNATURE_HERE"
    };

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "zzapcho-launcher-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}

internal static class Assert
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }
    }

    public static void True(bool value)
    {
        if (!value)
        {
            throw new InvalidOperationException("Expected true.");
        }
    }

    public static void False(bool value)
    {
        if (value)
        {
            throw new InvalidOperationException("Expected false.");
        }
    }

    public static void Contains(string expected, string actual)
    {
        if (!actual.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected text to contain '{expected}'.");
        }
    }
}
