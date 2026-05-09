using Zzapcho.Launcher.Core.Services;
using Zzapcho.Launcher.Core.Sync;

namespace Zzapcho.Launcher.App.ViewModels;

public sealed class UpdateViewModel : ObservableObject
{
    private readonly IFileSyncService _fileSyncService;
    private readonly IClientIntegrityState _integrityState;
    private readonly IAppLogger _logger;
    private FileSyncReport _report = new()
    {
        Status = FileCheckStatus.NotChecked,
        StatusText = "파일 확인 전"
    };

    public UpdateViewModel(IFileSyncService fileSyncService, IClientIntegrityState integrityState, IAppLogger logger)
    {
        _fileSyncService = fileSyncService;
        _integrityState = integrityState;
        _logger = logger;
        CheckCommand = new AsyncRelayCommand(() => RunCheckAsync(false));
        RepairCommand = new AsyncRelayCommand(() => RunCheckAsync(true));
        _ = RunCheckAsync(false);
    }

    public string Title => "업데이트";

    public string Status => _report.StatusText;

    public string Detail => "GitHub manifest에 적힌 Minecraft 버전, 로더 버전, 공식 파일 목록을 기준으로 검사합니다.";

    public string ManifestVersion => _report.ManifestVersion;

    public string ManifestHash => _report.ManifestHash;

    public string MinecraftVersion => _report.MinecraftVersion;

    public string LoaderVersion => _report.LoaderVersion;

    public int FilesChecked => _report.FilesChecked;

    public int FilesDownloaded => _report.FilesDownloaded;

    public int FilesMissing => _report.FilesMissing;

    public int FilesInvalid => _report.FilesInvalid;

    public int FilesQuarantined => _report.FilesQuarantined;

    public string Messages => _report.Messages.Count == 0
        ? "추가 메시지가 없습니다."
        : string.Join(Environment.NewLine, _report.Messages);

    public AsyncRelayCommand CheckCommand { get; }

    public AsyncRelayCommand RepairCommand { get; }

    private async Task RunCheckAsync(bool repair)
    {
        _integrityState.Set(new ClientIntegritySnapshot(false, true, "파일 확인 중", "클라이언트 파일을 확인하고 있습니다."));

        _report = new FileSyncReport
        {
            Status = FileCheckStatus.Checking,
            StatusText = repair ? "필요한 파일 다운로드 중" : "파일 확인 중"
        };
        RaiseReportProperties();

        _report = await _fileSyncService.CheckAsync(new FileSyncOptions { Repair = repair });
        RaiseReportProperties();

        if (_report.IsReady)
        {
            _integrityState.Set(new ClientIntegritySnapshot(true, false, "입장하기", "클라이언트 준비 완료"));
        }
        else
        {
            _integrityState.Set(new ClientIntegritySnapshot(false, false, "클라이언트 복구 필요", _report.StatusText));
        }

        _logger.Info($"업데이트 화면 검사 완료: {_report.StatusText}");
    }

    private void RaiseReportProperties()
    {
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(ManifestVersion));
        OnPropertyChanged(nameof(ManifestHash));
        OnPropertyChanged(nameof(MinecraftVersion));
        OnPropertyChanged(nameof(LoaderVersion));
        OnPropertyChanged(nameof(FilesChecked));
        OnPropertyChanged(nameof(FilesDownloaded));
        OnPropertyChanged(nameof(FilesMissing));
        OnPropertyChanged(nameof(FilesInvalid));
        OnPropertyChanged(nameof(FilesQuarantined));
        OnPropertyChanged(nameof(Messages));
    }
}
