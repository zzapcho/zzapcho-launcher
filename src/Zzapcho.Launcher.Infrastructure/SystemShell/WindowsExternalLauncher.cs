using System.Diagnostics;
using Zzapcho.Launcher.Core.Services;

namespace Zzapcho.Launcher.Infrastructure.SystemShell;

public sealed class WindowsExternalLauncher : IExternalLauncher
{
    public void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
