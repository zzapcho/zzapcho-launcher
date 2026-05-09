namespace Zzapcho.Launcher.Core.Manifest;

public sealed class ManifestValidationResult
{
    private ManifestValidationResult(IReadOnlyList<string> errors)
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public static ManifestValidationResult Success() => new(Array.Empty<string>());

    public static ManifestValidationResult Failure(IEnumerable<string> errors) => new(errors.ToArray());
}
