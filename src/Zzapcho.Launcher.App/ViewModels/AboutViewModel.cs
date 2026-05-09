using Zzapcho.Launcher.Core;

namespace Zzapcho.Launcher.App.ViewModels;

public sealed class AboutViewModel
{
    public AboutViewModel(LauncherPaths paths)
    {
        DataRoot = paths.DataRoot;
        InstanceRoot = paths.InstanceRoot;
        LogsRoot = paths.LogsRoot;
    }

    public string AppName => ProductConstants.KoreanName;

    public string InternalName => ProductConstants.EnglishName;

    public string Server => $"{ProductConstants.ServerHost}:{ProductConstants.ServerPort}";

    public string DataRoot { get; }

    public string InstanceRoot { get; }

    public string LogsRoot { get; }
}
