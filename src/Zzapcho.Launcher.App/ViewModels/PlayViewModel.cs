using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Models;
using Zzapcho.Launcher.Core.Services;
using Zzapcho.Launcher.Core.Sync;

namespace Zzapcho.Launcher.App.ViewModels;

public sealed class PlayViewModel : ObservableObject
{
    private readonly IServerStatusProvider _serverStatusProvider;
    private readonly IAppLogger _logger;
    private readonly IClientIntegrityState _integrityState;
    private readonly IAccountService _accountService;
    private readonly IFileSyncService _fileSyncService;
    private readonly IGamePreparationService _gamePreparationService;
    private readonly IGameLaunchService _gameLaunchService;
    private readonly IUpdateService _updateService;
    private ServerStatus _status = ServerStatus.Checking();
    private string _statusMessage = "서버 상태를 확인하고 있습니다.";
    private ClientIntegritySnapshot _integritySnapshot;

    public PlayViewModel(
        IServerStatusProvider serverStatusProvider,
        IAppLogger logger,
        IClientIntegrityState integrityState,
        IAccountService accountService,
        IFileSyncService fileSyncService,
        IGamePreparationService gamePreparationService,
        IGameLaunchService gameLaunchService,
        IUpdateService updateService)
    {
        _serverStatusProvider = serverStatusProvider;
        _logger = logger;
        _integrityState = integrityState;
        _accountService = accountService;
        _fileSyncService = fileSyncService;
        _gamePreparationService = gamePreparationService;
        _gameLaunchService = gameLaunchService;
        _updateService = updateService;
        _integritySnapshot = integrityState.Current;
        _integrityState.Changed += (_, snapshot) =>
        {
            _integritySnapshot = snapshot;
            OnPropertyChanged(nameof(PrimaryButtonText));
            OnPropertyChanged(nameof(IsPrimaryEnabled));
            OnPropertyChanged(nameof(ClientMessage));
        };
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        PlayCommand = new AsyncRelayCommand(PlayAsync);
        _ = RefreshLoopAsync();
    }

    public string Title => "잡초 약탈서버";

    public string Subtitle => "공식 클라이언트 구성을 확인한 뒤 입장합니다.";

    public string Host => ProductConstants.ServerHost;

    public string PrimaryButtonText => _integritySnapshot.IsReady ? "입장하기" : _integritySnapshot.PlayButtonText;

    public bool IsPrimaryEnabled => _integritySnapshot.IsReady;

    public string ClientMessage => _integritySnapshot.Message;

    public string PlayerListFallback => "접속자 목록은 서버 API 연결 후 표시됩니다.";

    public AsyncRelayCommand RefreshCommand { get; }

    public AsyncRelayCommand PlayCommand { get; }

    public ServerStatus Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(PlayerSummary));
                OnPropertyChanged(nameof(LatencyText));
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string PlayerSummary => $"{Status.CurrentPlayers} / {Status.MaxPlayers}";

    public string LatencyText => Status.LatencyMs.HasValue ? $"{Status.LatencyMs.Value} ms" : "-";

    public async Task RefreshAsync()
    {
        Status = ServerStatus.Checking();
        StatusMessage = "서버 상태를 확인하고 있습니다.";

        try
        {
            var next = await _serverStatusProvider.GetStatusAsync();
            Status = next;
            StatusMessage = next.State == ServerStatusState.Online
                ? "서버가 열려 있습니다."
                : "서버 상태를 확인하지 못했습니다.";
            _logger.Info($"서버 상태 확인: {next.StateText}, {next.CurrentPlayers}/{next.MaxPlayers}");
        }
        catch (Exception ex)
        {
            Status = ServerStatus.Offline("서버 상태를 확인하지 못했습니다.");
            StatusMessage = "서버 상태를 확인하지 못했습니다.";
            _logger.Error("서버 상태 확인 중 오류가 발생했습니다.", ex);
        }
    }

    private async Task RefreshLoopAsync()
    {
        await RefreshAsync();

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync())
        {
            await RefreshAsync();
        }
    }

    private async Task PlayAsync()
    {
        var account = await _accountService.GetCurrentAsync();
        if (!account.IsLoggedIn)
        {
            _integrityState.Set(new ClientIntegritySnapshot(false, false, "로그인 필요", "Microsoft 로그인이 필요합니다."));
            return;
        }

        _integrityState.Set(new ClientIntegritySnapshot(false, true, "파일 확인 중", "클라이언트 파일을 확인하고 있습니다."));
        var update = await _updateService.CheckAsync();
        if (update.Mandatory)
        {
            _integrityState.Set(new ClientIntegritySnapshot(false, false, "업데이트 필요", update.Message));
            return;
        }

        var sync = await _fileSyncService.CheckAsync(new Core.Sync.FileSyncOptions { Repair = true });
        if (!sync.IsReady)
        {
            _integrityState.Set(new ClientIntegritySnapshot(false, false, "클라이언트 복구 필요", sync.StatusText));
            return;
        }

        _integrityState.Set(new ClientIntegritySnapshot(false, true, "실행 환경 준비 중", "Java와 Minecraft 실행 환경을 준비합니다."));
        var prep = await _gamePreparationService.PrepareAsync();
        if (!prep.Success)
        {
            _integrityState.Set(new ClientIntegritySnapshot(false, false, "오류 발생", prep.Message));
            return;
        }

        _integrityState.Set(new ClientIntegritySnapshot(false, true, "게임 시작 중", "Minecraft를 실행합니다."));
        var launch = await _gameLaunchService.LaunchAsync();
        _integrityState.Set(new ClientIntegritySnapshot(launch.Success, false, launch.Success ? "실행 중" : "오류 발생", launch.Message));
    }
}
