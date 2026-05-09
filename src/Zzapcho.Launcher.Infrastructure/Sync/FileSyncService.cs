using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Manifest;
using Zzapcho.Launcher.Core.Services;
using Zzapcho.Launcher.Core.Sync;
using Zzapcho.Launcher.Infrastructure.Manifest;

namespace Zzapcho.Launcher.Infrastructure.Sync;

public sealed class FileSyncService : IFileSyncService
{
    private readonly LauncherPaths _paths;
    private readonly ISettingsService _settingsService;
    private readonly IManifestDownloader _manifestDownloader;
    private readonly ManifestParser _parser;
    private readonly ManifestValidator _validator;
    private readonly ManifestSignatureVerifier _signatureVerifier;
    private readonly IManifestHashService _manifestHashService;
    private readonly FileHashVerifier _hashVerifier;
    private readonly FileDownloader _fileDownloader;
    private readonly QuarantineService _quarantineService;
    private readonly IAppLogger _logger;

    public FileSyncService(
        LauncherPaths paths,
        ISettingsService settingsService,
        IManifestDownloader manifestDownloader,
        ManifestParser parser,
        ManifestValidator validator,
        ManifestSignatureVerifier signatureVerifier,
        IManifestHashService manifestHashService,
        FileHashVerifier hashVerifier,
        FileDownloader fileDownloader,
        QuarantineService quarantineService,
        IAppLogger logger)
    {
        _paths = paths;
        _settingsService = settingsService;
        _manifestDownloader = manifestDownloader;
        _parser = parser;
        _validator = validator;
        _signatureVerifier = signatureVerifier;
        _manifestHashService = manifestHashService;
        _hashVerifier = hashVerifier;
        _fileDownloader = fileDownloader;
        _quarantineService = quarantineService;
        _logger = logger;
    }

    public async Task<FileSyncReport> CheckAsync(FileSyncOptions options, CancellationToken cancellationToken = default)
    {
        _paths.EnsureCreated();
        var messages = new List<string>();

        try
        {
            var settings = await _settingsService.LoadAsync(cancellationToken);
            var manifestJson = await _manifestDownloader.DownloadOrLoadAsync(settings.ManifestUrl, cancellationToken);
            var manifest = _parser.Parse(manifestJson);
            var manifestHash = _manifestHashService.ComputeHash(manifestJson);

            var validation = _validator.Validate(manifest);
            if (!validation.IsValid)
            {
                return Report(FileCheckStatus.Failed, "manifest 검증 실패", manifest, manifestHash, messages.Concat(validation.Errors));
            }

            var signature = _signatureVerifier.Verify(manifestJson, manifest, options.AllowDevelopmentManifestPlaceholder);
            messages.Add(signature.Message);
            if (!signature.IsValid)
            {
                return Report(FileCheckStatus.Failed, "manifest 서명 검증 실패", manifest, manifestHash, messages);
            }

            var checkedCount = 0;
            var downloadedCount = 0;
            var missingCount = 0;
            var invalidCount = 0;

            foreach (var file in manifest.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                checkedCount++;
                var absolute = ResolveInstancePath(file.Path);
                var exists = File.Exists(absolute);
                var valid = exists && await _hashVerifier.VerifyAsync(absolute, file.Sha256, cancellationToken);

                if (!exists)
                {
                    missingCount++;
                }
                else if (!valid)
                {
                    invalidCount++;
                }

                if ((!exists || !valid) && options.Repair)
                {
                    await _fileDownloader.DownloadAtomicAsync(file.Url, absolute, cancellationToken);
                    if (!await _hashVerifier.VerifyAsync(absolute, file.Sha256, cancellationToken))
                    {
                        invalidCount++;
                        messages.Add($"다운로드 후 해시 검증 실패: {file.Path}");
                    }
                    else
                    {
                        downloadedCount++;
                    }
                }
            }

            var quarantined = _quarantineService.QuarantineUnknownFiles(manifest);
            if (quarantined > 0)
            {
                messages.Add("공식 클라이언트 구성과 다른 파일을 정리했습니다.");
            }

            var ready = missingCount == 0 && invalidCount == 0;
            var status = ready ? FileCheckStatus.Ready : FileCheckStatus.RepairRequired;
            var statusText = ready ? "클라이언트 준비 완료" : "클라이언트 복구 필요";
            _logger.Info($"파일 검사 완료: {statusText}, checked={checkedCount}, missing={missingCount}, invalid={invalidCount}, quarantined={quarantined}");

            return new FileSyncReport
            {
                Status = status,
                StatusText = statusText,
                ManifestVersion = manifest.ManifestVersion,
                ManifestHash = manifestHash,
                MinecraftVersion = manifest.Minecraft.Version,
                LoaderVersion = $"{manifest.Minecraft.Loader} {manifest.Minecraft.LoaderVersion}",
                FilesChecked = checkedCount,
                FilesDownloaded = downloadedCount,
                FilesMissing = missingCount,
                FilesInvalid = invalidCount,
                FilesQuarantined = quarantined,
                Messages = messages
            };
        }
        catch (Exception ex)
        {
            _logger.Error("파일 검사 중 오류가 발생했습니다.", ex);
            return new FileSyncReport
            {
                Status = FileCheckStatus.Failed,
                StatusText = "파일 확인 실패",
                Messages = new[] { ex.Message }
            };
        }
    }

    private string ResolveInstancePath(string manifestPath)
    {
        if (!ManifestValidator.IsSafeRelativePath(manifestPath))
        {
            throw new InvalidDataException($"안전하지 않은 파일 경로입니다: {manifestPath}");
        }

        var fullPath = Path.GetFullPath(Path.Combine(_paths.InstanceRoot, ManifestValidator.NormalizeManifestPath(manifestPath)));
        var root = Path.GetFullPath(_paths.InstanceRoot);
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"인스턴스 폴더 밖의 파일은 허용하지 않습니다: {manifestPath}");
        }

        return fullPath;
    }

    private static FileSyncReport Report(
        FileCheckStatus status,
        string statusText,
        LauncherManifest manifest,
        string manifestHash,
        IEnumerable<string> messages)
    {
        return new FileSyncReport
        {
            Status = status,
            StatusText = statusText,
            ManifestVersion = manifest.ManifestVersion,
            ManifestHash = manifestHash,
            MinecraftVersion = manifest.Minecraft.Version,
            LoaderVersion = $"{manifest.Minecraft.Loader} {manifest.Minecraft.LoaderVersion}",
            Messages = messages.ToArray()
        };
    }
}
