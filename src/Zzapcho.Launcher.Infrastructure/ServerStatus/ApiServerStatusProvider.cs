using System.Text.Json;
using System.Text.Json.Nodes;
using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Models;
using Zzapcho.Launcher.Core.Services;
using StatusModel = Zzapcho.Launcher.Core.Models.ServerStatus;

namespace Zzapcho.Launcher.Infrastructure.ServerStatus;

public sealed class ApiServerStatusProvider : IServerStatusProvider
{
    private readonly HttpClient _httpClient;
    private readonly Uri _statusUri;

    public ApiServerStatusProvider(HttpClient httpClient, Uri statusUri)
    {
        _httpClient = httpClient;
        _statusUri = statusUri;
    }

    public async Task<StatusModel> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(_statusUri, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var node = JsonNode.Parse(json);
            if (node is null)
            {
                return StatusModel.Offline("서버 상태 응답을 읽지 못했습니다.");
            }

            var players = node["players"]?["list"]?.AsArray()
                .OfType<JsonNode>()
                .Select(p => new PlayerSample(p["name"]?.GetValue<string>() ?? "-", p["uuid"]?.GetValue<string>()))
                .ToArray() ?? Array.Empty<PlayerSample>();

            return new StatusModel
            {
                State = node["online"]?.GetValue<bool>() == true ? ServerStatusState.Online : ServerStatusState.Offline,
                Host = node["host"]?.GetValue<string>() ?? ProductConstants.ServerHost,
                Port = node["port"]?.GetValue<int>() ?? ProductConstants.ServerPort,
                Version = node["version"]?.GetValue<string>() ?? "-",
                Motd = node["motd"]?.GetValue<string>() ?? "-",
                CurrentPlayers = node["players"]?["online"]?.GetValue<int>() ?? 0,
                MaxPlayers = node["players"]?["max"]?.GetValue<int>() ?? 0,
                Players = players,
                LatencyMs = node["latencyMs"]?.GetValue<long>()
            };
        }
        catch (Exception ex)
        {
            return StatusModel.Offline(ex.Message);
        }
    }
}
