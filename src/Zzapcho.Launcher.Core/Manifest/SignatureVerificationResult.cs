namespace Zzapcho.Launcher.Core.Manifest;

public sealed class SignatureVerificationResult
{
    private SignatureVerificationResult(bool isValid, bool isDevelopmentPlaceholder, string message)
    {
        IsValid = isValid;
        IsDevelopmentPlaceholder = isDevelopmentPlaceholder;
        Message = message;
    }

    public bool IsValid { get; }

    public bool IsDevelopmentPlaceholder { get; }

    public string Message { get; }

    public static SignatureVerificationResult Valid() => new(true, false, "manifest 서명이 유효합니다.");

    public static SignatureVerificationResult DevelopmentPlaceholder() => new(true, true, "개발용 placeholder manifest입니다. 운영 배포 전 실제 서명이 필요합니다.");

    public static SignatureVerificationResult Invalid(string message) => new(false, false, message);
}
