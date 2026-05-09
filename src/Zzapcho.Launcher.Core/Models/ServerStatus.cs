namespace Zzapcho.Launcher.Core.Models;

public sealed class ServerStatus
{
    public static ServerStatus Checking() => new()
    {
        State = ServerStatusState.Checking,
        Host = ProductConstants.ServerHost,
        Port = ProductConstants.ServerPort
    };

    public static ServerStatus Offline(string message) => new()
    {
        State = ServerStatusState.Offline,
        Host = ProductConstants.ServerHost,
        Port = ProductConstants.ServerPort,
        ErrorMessage = message
    };

    public ServerStatusState State { get; init; }

    public string Host { get; init; } = ProductConstants.ServerHost;

    public int Port { get; init; } = ProductConstants.ServerPort;

    public int CurrentPlayers { get; init; }

    public int MaxPlayers { get; init; }

    public string Version { get; init; } = "-";

    public string Motd { get; init; } = "-";

    public long? LatencyMs { get; init; }

    public IReadOnlyList<PlayerSample> Players { get; init; } = Array.Empty<PlayerSample>();

    public string? ErrorMessage { get; init; }

    public bool HasPlayerSamples => Players.Count > 0;

    public string StateText => State switch
    {
        ServerStatusState.Online => "온라인",
        ServerStatusState.Offline => "오프라인",
        ServerStatusState.Maintenance => "점검 중",
        _ => "확인 중"
    };
}
