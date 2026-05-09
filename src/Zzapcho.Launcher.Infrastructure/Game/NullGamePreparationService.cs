using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.Infrastructure.Game;

public sealed class NullGamePreparationService : IGamePreparationService
{
    public Task<PreparationResult> PrepareAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PreparationResult(true, "실행 환경 준비는 3번 단계 구현체에서 실제 연결됩니다."));
    }
}
