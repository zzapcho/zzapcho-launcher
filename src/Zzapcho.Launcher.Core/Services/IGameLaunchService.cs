namespace Zzapcho.Launcher.Core.Services;

public interface IGameLaunchService
{
    Task<LaunchResult> LaunchAsync(CancellationToken cancellationToken = default);
}

public sealed record LaunchResult(bool Success, string Message, int? ExitCode = null);
