using System.Windows;
using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.App.ViewModels;

public sealed class LogsViewModel : ObservableObject
{
    private readonly LauncherPaths _paths;
    private readonly ILogReader _logReader;
    private readonly IAppLogger _logger;
    private readonly IExternalLauncher _externalLauncher;
    private readonly ICrashSupportService _crashSupportService;
    private string _selectedLogName = "launcher";
    private string _searchText = string.Empty;
    private string _logText = "로그를 불러오는 중입니다.";
    private string _supportZipStatus = "지원 ZIP은 이후 단계에서 실제 압축 생성으로 연결됩니다.";

    public LogsViewModel(LauncherPaths paths, ILogReader logReader, IAppLogger logger, IExternalLauncher externalLauncher, ICrashSupportService crashSupportService)
    {
        _paths = paths;
        _logReader = logReader;
        _logger = logger;
        _externalLauncher = externalLauncher;
        _crashSupportService = crashSupportService;
        LoadLogCommand = new AsyncRelayCommand(LoadLogAsync);
        CopyCommand = new RelayCommand(CopyFilteredLog);
        OpenLogsFolderCommand = new RelayCommand(() => _externalLauncher.OpenFolder(_paths.LogsRoot));
        CreateSupportZipCommand = new AsyncRelayCommand(CreateSupportZipAsync);
        _ = LoadLogAsync();
    }

    public IReadOnlyList<string> LogNames { get; } = new[] { "launcher", "game", "crash" };

    public string SelectedLogName
    {
        get => _selectedLogName;
        set
        {
            if (SetProperty(ref _selectedLogName, value))
            {
                _ = LoadLogAsync();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OnPropertyChanged(nameof(FilteredLogText));
            }
        }
    }

    public string LogText
    {
        get => _logText;
        private set
        {
            if (SetProperty(ref _logText, value))
            {
                OnPropertyChanged(nameof(FilteredLogText));
            }
        }
    }

    public string FilteredLogText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return LogText;
            }

            var lines = LogText.Split(Environment.NewLine);
            return string.Join(Environment.NewLine, lines.Where(line => line.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public string SupportZipStatus
    {
        get => _supportZipStatus;
        private set => SetProperty(ref _supportZipStatus, value);
    }

    public AsyncRelayCommand LoadLogCommand { get; }

    public RelayCommand CopyCommand { get; }

    public RelayCommand OpenLogsFolderCommand { get; }

    public AsyncRelayCommand CreateSupportZipCommand { get; }

    private async Task LoadLogAsync()
    {
        try
        {
            LogText = await _logReader.ReadLogAsync(SelectedLogName);
        }
        catch (Exception ex)
        {
            LogText = "로그를 읽지 못했습니다.";
            _logger.Error("로그 읽기 실패", ex);
        }
    }

    private void CopyFilteredLog()
    {
        Clipboard.SetText(FilteredLogText);
        _logger.Info("로그 내용을 클립보드에 복사했습니다.");
    }

    private async Task CreateSupportZipAsync()
    {
        var path = await _crashSupportService.CreateSupportZipAsync();
        SupportZipStatus = $"지원 파일을 만들었습니다: {path}";
        _logger.Info($"지원 ZIP 생성: {path}");
    }
}
