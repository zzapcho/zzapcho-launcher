namespace Zzapcho.Launcher.Core.Sync;

public sealed record ClientIntegritySnapshot(
    bool IsReady,
    bool IsChecking,
    string PlayButtonText,
    string Message)
{
    public static ClientIntegritySnapshot Initial { get; } = new(false, false, "파일 확인 필요", "클라이언트 파일 검사가 필요합니다.");
}
