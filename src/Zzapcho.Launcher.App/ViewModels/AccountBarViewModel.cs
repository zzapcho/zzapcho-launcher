using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.App.ViewModels;

public sealed class AccountBarViewModel : ObservableObject
{
    private readonly IAccountService _accountService;
    private readonly IAppLogger _logger;
    private string _playerName = "Player";
    private string _status = "로그인 전";
    private bool _isLoggedIn;

    public AccountBarViewModel(IAccountService accountService, IAppLogger logger)
    {
        _accountService = accountService;
        _logger = logger;
        LoginCommand = new AsyncRelayCommand(LoginAsync);
        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        _ = LoadAsync();
    }

    public string PlayerName
    {
        get => _playerName;
        private set => SetProperty(ref _playerName, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        private set => SetProperty(ref _isLoggedIn, value);
    }

    public AsyncRelayCommand LoginCommand { get; }

    public AsyncRelayCommand LogoutCommand { get; }

    private async Task LoadAsync()
    {
        var account = await _accountService.GetCurrentAsync();
        Apply(account.IsLoggedIn, account.PlayerName);
    }

    private async Task LoginAsync()
    {
        var account = await _accountService.LoginAsync();
        Apply(account.IsLoggedIn, account.PlayerName);
        _logger.Info($"개발용 계정 로그인 상태: {account.PlayerName}");
    }

    private async Task LogoutAsync()
    {
        await _accountService.LogoutAsync();
        Apply(false, "Player");
        _logger.Info("로그아웃했습니다.");
    }

    private void Apply(bool isLoggedIn, string playerName)
    {
        IsLoggedIn = isLoggedIn;
        PlayerName = playerName;
        Status = isLoggedIn ? "로그인됨" : "로그인 전";
    }
}
