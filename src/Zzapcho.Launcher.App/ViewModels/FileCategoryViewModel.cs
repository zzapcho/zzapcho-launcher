using System.Collections.ObjectModel;
using Zzapcho.Launcher.Core.Models;
using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.App.ViewModels;

public sealed class FileCategoryViewModel : ObservableObject
{
    private readonly IFileInventoryService _fileInventoryService;
    private string _status = "파일 목록을 불러오는 중입니다.";

    public FileCategoryViewModel(string title, string category, string dropText, IFileInventoryService fileInventoryService)
    {
        Title = title;
        Category = category;
        DropText = dropText;
        _fileInventoryService = fileInventoryService;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        BrowseCommand = new RelayCommand(() => Status = "Modrinth 브라우저는 이후 확장 단계에서 연결됩니다.");
        AddFileCommand = new RelayCommand(() => Status = "공식 파일은 GitHub manifest에서 관리합니다. 임의 파일 추가는 허용하지 않습니다.");
        _ = RefreshAsync();
    }

    public string Title { get; }

    public string Category { get; }

    public string DropText { get; }

    public ObservableCollection<ManagedFileItem> ServerFiles { get; } = new();

    public ObservableCollection<ManagedFileItem> UserFiles { get; } = new();

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public AsyncRelayCommand RefreshCommand { get; }

    public RelayCommand BrowseCommand { get; }

    public RelayCommand AddFileCommand { get; }

    private async Task RefreshAsync()
    {
        ServerFiles.Clear();
        UserFiles.Clear();
        foreach (var item in await _fileInventoryService.ListAsync(Category))
        {
            if (item.IsServerFile)
            {
                ServerFiles.Add(item);
            }
            else
            {
                UserFiles.Add(item);
            }
        }

        Status = ServerFiles.Count == 0 ? "아직 manifest로 받은 파일이 없습니다." : $"{ServerFiles.Count}개 파일";
    }
}
