namespace Zzapcho.Launcher.Core;

public sealed class LauncherPaths
{
    public LauncherPaths(string? localAppData = null)
    {
        var root = localAppData ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        DataRoot = Path.Combine(root, ProductConstants.DataFolderName);
        InstanceRoot = Path.Combine(DataRoot, "instances", "main");
        LogsRoot = Path.Combine(DataRoot, "logs");
        CrashesRoot = Path.Combine(DataRoot, "crashes");
        QuarantineRoot = Path.Combine(DataRoot, "quarantine");
        SettingsFile = Path.Combine(DataRoot, "settings.json");
    }

    public string DataRoot { get; }

    public string InstanceRoot { get; }

    public string LogsRoot { get; }

    public string CrashesRoot { get; }

    public string QuarantineRoot { get; }

    public string SettingsFile { get; }

    public string LauncherLogFile => Path.Combine(LogsRoot, "launcher.log");

    public string GameLogFile => Path.Combine(LogsRoot, "game.log");

    public string UpdateLogFile => Path.Combine(LogsRoot, "update.log");

    public string CrashLogFile => Path.Combine(LogsRoot, "crash.log");

    public void EnsureCreated()
    {
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(InstanceRoot);
        Directory.CreateDirectory(LogsRoot);
        Directory.CreateDirectory(CrashesRoot);
        Directory.CreateDirectory(QuarantineRoot);
    }
}
