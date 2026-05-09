namespace Zzapcho.Launcher.Core.Services;

public interface ILogReader
{
    Task<string> ReadLogAsync(string logName, CancellationToken cancellationToken = default);
}
