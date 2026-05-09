using System.Text.Json;
using Zzapcho.Launcher.Core.Manifest;

namespace Zzapcho.Launcher.Infrastructure.Manifest;

public sealed class ManifestParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public LauncherManifest Parse(string json)
    {
        return JsonSerializer.Deserialize<LauncherManifest>(json, JsonOptions)
            ?? throw new InvalidDataException("manifest 내용을 읽지 못했습니다.");
    }
}
