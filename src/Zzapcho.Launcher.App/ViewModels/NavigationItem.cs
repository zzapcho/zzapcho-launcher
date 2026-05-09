namespace Zzapcho.Launcher.App.ViewModels;

public sealed class NavigationItem
{
    public NavigationItem(string title, object viewModel)
    {
        Title = title;
        ViewModel = viewModel;
    }

    public string Title { get; }

    public object ViewModel { get; }
}
