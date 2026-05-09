namespace Zzapcho.Launcher.Core.Models;

public sealed record AccountState(bool IsLoggedIn, string PlayerName, string? Uuid)
{
    public static AccountState SignedOut { get; } = new(false, "Player", null);
}
