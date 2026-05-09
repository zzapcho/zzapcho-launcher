using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Models;
using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject
{
    private readonly LauncherPaths _paths;
    private readonly ISettingsService _settingsService;
    private readonly IAppLogger _logger;
    private readonly IExternalLauncher _externalLauncher;
    private LauncherSettings _settings = LauncherSettings.CreateDefault();
    private string _status = "설정을 불러오는 중입니다.";

    public SettingsViewModel(LauncherPaths paths, ISettingsService settingsService, IAppLogger logger, IExternalLauncher externalLauncher)
    {
        _paths = paths;
        _settingsService = settingsService;
        _logger = logger;
        _externalLauncher = externalLauncher;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ResetCommand = new AsyncRelayCommand(ResetAsync);
        OpenDataFolderCommand = new RelayCommand(() => _externalLauncher.OpenFolder(_paths.DataRoot));
        _ = LoadAsync();
    }

    public int RamMinMb
    {
        get => _settings.RamMinMb;
        set
        {
            if (_settings.RamMinMb != value)
            {
                _settings.RamMinMb = value;
                OnPropertyChanged();
            }
        }
    }

    public int RamMaxMb
    {
        get => _settings.RamMaxMb;
        set
        {
            if (_settings.RamMaxMb != value)
            {
                _settings.RamMaxMb = value;
                OnPropertyChanged();
            }
        }
    }

    public bool AutoUpdate
    {
        get => _settings.AutoUpdate;
        set
        {
            if (_settings.AutoUpdate != value)
            {
                _settings.AutoUpdate = value;
                OnPropertyChanged();
            }
        }
    }

    public bool CrashReportConsent
    {
        get => _settings.CrashReportConsent;
        set
        {
            if (_settings.CrashReportConsent != value)
            {
                _settings.CrashReportConsent = value;
                OnPropertyChanged();
            }
        }
    }

    public string ManifestUrl
    {
        get => _settings.ManifestUrl;
        set
        {
            if (_settings.ManifestUrl != value)
            {
                _settings.ManifestUrl = value;
                OnPropertyChanged();
            }
        }
    }

    public string LauncherUpdateSourceUrl
    {
        get => _settings.LauncherUpdateSourceUrl;
        set
        {
            if (_settings.LauncherUpdateSourceUrl != value)
            {
                _settings.LauncherUpdateSourceUrl = value;
                OnPropertyChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public AsyncRelayCommand SaveCommand { get; }

    public AsyncRelayCommand ResetCommand { get; }

    public RelayCommand OpenDataFolderCommand { get; }

    private async Task LoadAsync()
    {
        _settings = await _settingsService.LoadAsync();
        OnPropertyChanged(nameof(RamMinMb));
        OnPropertyChanged(nameof(RamMaxMb));
        OnPropertyChanged(nameof(AutoUpdate));
        OnPropertyChanged(nameof(CrashReportConsent));
        OnPropertyChanged(nameof(ManifestUrl));
        OnPropertyChanged(nameof(LauncherUpdateSourceUrl));
        Status = "설정을 불러왔습니다.";
    }

    private async Task SaveAsync()
    {
        await _settingsService.SaveAsync(_settings);
        Status = "설정을 저장했습니다.";
        _logger.Info("설정을 저장했습니다.");
    }

    private async Task ResetAsync()
    {
        _settings = await _settingsService.ResetAsync();
        OnPropertyChanged(nameof(RamMinMb));
        OnPropertyChanged(nameof(RamMaxMb));
        OnPropertyChanged(nameof(AutoUpdate));
        OnPropertyChanged(nameof(CrashReportConsent));
        OnPropertyChanged(nameof(ManifestUrl));
        OnPropertyChanged(nameof(LauncherUpdateSourceUrl));
        Status = "기본 설정으로 되돌렸습니다.";
        _logger.Info("설정을 기본값으로 되돌렸습니다.");
    }
}
