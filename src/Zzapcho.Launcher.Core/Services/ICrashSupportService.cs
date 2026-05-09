namespace Zzapcho.Launcher.Core.Services;

public interface ICrashSupportService
{
    Task<string> CreateSupportZipAsync(CancellationToken cancellationToken = default);
}
