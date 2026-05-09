using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.Infrastructure.Logging;

public sealed class LogReader : ILogReader
{
    private readonly LauncherPaths _paths;

    public LogReader(LauncherPaths paths)
    {
        _paths = paths;
    }

    public async Task<string> ReadLogAsync(string logName, CancellationToken cancellationToken = default)
    {
        var file = logName.ToLowerInvariant() switch
        {
            "game" => _paths.GameLogFile,
            "update" => _paths.UpdateLogFile,
            "crash" => _paths.CrashLogFile,
            _ => _paths.LauncherLogFile
        };

        if (!File.Exists(file))
        {
            return "아직 기록된 로그가 없습니다.";
        }

        return await File.ReadAllTextAsync(file, cancellationToken);
    }
}
