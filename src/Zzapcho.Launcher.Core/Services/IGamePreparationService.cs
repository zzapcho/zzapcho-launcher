namespace Zzapcho.Launcher.Core.Services;

public interface IGamePreparationService
{
    Task<PreparationResult> PrepareAsync(CancellationToken cancellationToken = default);
}

public sealed record PreparationResult(bool Success, string Message);
