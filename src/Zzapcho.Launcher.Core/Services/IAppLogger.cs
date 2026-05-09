namespace Zzapcho.Launcher.Core.Services;

public interface IAppLogger
{
    void Info(string message);

    void Warning(string message);

    void Error(string message, Exception? exception = null);

    void AppendGameLog(string message);
}
