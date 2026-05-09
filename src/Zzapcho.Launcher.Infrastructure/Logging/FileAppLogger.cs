using System.Text;
using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.Infrastructure.Logging;

public sealed class FileAppLogger : IAppLogger
{
    private readonly LauncherPaths _paths;
    private readonly object _sync = new();

    public FileAppLogger(LauncherPaths paths)
    {
        _paths = paths;
        _paths.EnsureCreated();
    }

    public void Info(string message) => Write(_paths.LauncherLogFile, "INFO", message, null);

    public void Warning(string message) => Write(_paths.LauncherLogFile, "WARN", message, null);

    public void Error(string message, Exception? exception = null) => Write(_paths.LauncherLogFile, "ERROR", message, exception);

    public void AppendGameLog(string message) => Write(_paths.GameLogFile, "GAME", message, null);

    private void Write(string file, string level, string message, Exception? exception)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {Redact(message)}";
        if (exception is not null)
        {
            line += Environment.NewLine + Redact(exception.ToString());
        }

        lock (_sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.AppendAllText(file, line + Environment.NewLine, Encoding.UTF8);
        }
    }

    private static string Redact(string value)
    {
        var redacted = value;
        foreach (var marker in new[] { "access_token", "refresh_token", "Authorization:", "Bearer " })
        {
            redacted = redacted.Replace(marker, "[redacted]", StringComparison.OrdinalIgnoreCase);
        }

        return redacted;
    }
}
