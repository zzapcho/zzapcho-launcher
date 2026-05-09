using System.Text.RegularExpressions;
using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Manifest;

namespace Zzapcho.Launcher.Infrastructure.Manifest;

public sealed partial class ManifestValidator
{
    public ManifestValidationResult Validate(LauncherManifest manifest)
    {
        var errors = new List<string>();

        if (manifest.SchemaVersion != 1)
        {
            errors.Add("지원하지 않는 manifest schemaVersion입니다.");
        }

        Require(manifest.ProfileId, "profileId", errors);
        Require(manifest.DisplayName, "displayName", errors);
        Require(manifest.ManifestVersion, "manifestVersion", errors);

        if (!string.Equals(manifest.Server.Host, ProductConstants.ServerHost, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("manifest의 서버 주소가 런처 고정 서버와 다릅니다.");
        }

        if (manifest.Server.Port != ProductConstants.ServerPort)
        {
            errors.Add("manifest의 서버 포트가 런처 고정 포트와 다릅니다.");
        }

        Require(manifest.Minecraft.Version, "minecraft.version", errors);
        Require(manifest.Minecraft.Loader, "minecraft.loader", errors);
        Require(manifest.Minecraft.LoaderVersion, "minecraft.loaderVersion", errors);
        Require(manifest.Launcher.MinimumVersion, "launcher.minimumVersion", errors);
        Require(manifest.Launcher.LatestVersion, "launcher.latestVersion", errors);

        if (manifest.Sync.ProtectedDirectories.Count == 0)
        {
            errors.Add("protectedDirectories가 비어 있습니다.");
        }

        foreach (var directory in manifest.Sync.ProtectedDirectories)
        {
            if (!IsSafeRelativePath(directory))
            {
                errors.Add($"보호 폴더 경로가 안전하지 않습니다: {directory}");
            }
        }

        var duplicatePaths = manifest.Files
            .GroupBy(file => NormalizeManifestPath(file.Path))
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var duplicate in duplicatePaths)
        {
            errors.Add($"manifest 파일 경로가 중복되었습니다: {duplicate}");
        }

        foreach (var file in manifest.Files)
        {
            ValidateFile(file, errors);
        }

        return errors.Count == 0
            ? ManifestValidationResult.Success()
            : ManifestValidationResult.Failure(errors);
    }

    public static bool IsSafeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (Path.IsPathRooted(path))
        {
            return false;
        }

        var normalized = NormalizeManifestPath(path);
        if (normalized.StartsWith("../", StringComparison.Ordinal) ||
            normalized.Contains("/../", StringComparison.Ordinal) ||
            normalized.Equals("..", StringComparison.Ordinal))
        {
            return false;
        }

        return !normalized.Contains(':') && !normalized.Contains('\\');
    }

    public static string NormalizeManifestPath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    private static void ValidateFile(ManifestFile file, List<string> errors)
    {
        if (!IsSafeRelativePath(file.Path))
        {
            errors.Add($"파일 경로가 안전하지 않습니다: {file.Path}");
        }

        if (string.IsNullOrWhiteSpace(file.Url) || !Uri.TryCreate(file.Url, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("https" or "http"))
        {
            errors.Add($"파일 URL이 올바르지 않습니다: {file.Path}");
        }

        if (!IsSha256(file.Sha256) && !IsDevelopmentPlaceholder(file.Sha256))
        {
            errors.Add($"SHA-256 값이 올바르지 않습니다: {file.Path}");
        }

        if (file.Size < 0)
        {
            errors.Add($"파일 크기가 올바르지 않습니다: {file.Path}");
        }
    }

    private static void Require(string value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{name} 값이 비어 있습니다.");
        }
    }

    private static bool IsSha256(string value) => Sha256Regex().IsMatch(value);

    private static bool IsDevelopmentPlaceholder(string value) =>
        value.Equals("PUT_SHA256_HERE", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("^[a-fA-F0-9]{64}$")]
    private static partial Regex Sha256Regex();
}
