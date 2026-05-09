using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.Infrastructure.Game;

public sealed class NullGameLaunchService : IGameLaunchService
{
    public Task<LaunchResult> LaunchAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LaunchResult(false, "Minecraft 실행은 Microsoft 로그인과 CMLLib 연결 후 활성화됩니다."));
    }
}
