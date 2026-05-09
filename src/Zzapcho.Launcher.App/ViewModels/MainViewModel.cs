using System.Collections.ObjectModel;
using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private NavigationItem _selectedNavigationItem;

    public MainViewModel(
        LauncherPaths paths,
        ISettingsService settingsService,
        ILogReader logReader,
        IAppLogger logger,
        IExternalLauncher externalLauncher,
        IServerStatusProvider serverStatusProvider,
        IFileSyncService fileSyncService,
        IClientIntegrityState integrityState,
        IAccountService accountService,
        IFileInventoryService fileInventoryService,
        IGamePreparationService gamePreparationService,
        IGameLaunchService gameLaunchService,
        IUpdateService updateService,
        ICrashSupportService crashSupportService)
    {
        Account = new AccountBarViewModel(accountService, logger);
        var play = new PlayViewModel(serverStatusProvider, logger, integrityState, accountService, fileSyncService, gamePreparationService, gameLaunchService, updateService);
        var mods = new FileCategoryViewModel("모드", "mods", ".jar 파일을 여기에 드래그하세요", fileInventoryService);
        var resourcepacks = new FileCategoryViewModel("리소스팩", "resourcepacks", ".zip 파일을 여기에 드래그하세요", fileInventoryService);
        var shaderpacks = new FileCategoryViewModel("셰이더", "shaderpacks", ".zip / .jar 파일을 여기에 드래그하세요", fileInventoryService);
        var settings = new SettingsViewModel(paths, settingsService, logger, externalLauncher);
        var logs = new LogsViewModel(paths, logReader, logger, externalLauncher, crashSupportService);

        NavigationItems = new ObservableCollection<NavigationItem>
        {
            new("홈", play),
            new("모드", mods),
            new("리소스팩", resourcepacks),
            new("셰이더", shaderpacks),
            new("설정", settings),
            new("로그", logs)
        };

        _selectedNavigationItem = NavigationItems[0];
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public AccountBarViewModel Account { get; }

    public NavigationItem SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (SetProperty(ref _selectedNavigationItem, value))
            {
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        }
    }

    public object CurrentViewModel => SelectedNavigationItem.ViewModel;
}
