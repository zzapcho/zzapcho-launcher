using System.Buffers.Binary;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Zzapcho.Launcher.Core;
using Zzapcho.Launcher.Core.Models;
using Zzapcho.Launcher.Core.Services;
using StatusModel = Zzapcho.Launcher.Core.Models.ServerStatus;

namespace Zzapcho.Launcher.Infrastructure.ServerStatus;

public sealed class MinecraftPingStatusProvider : IServerStatusProvider
{
    private const int ProtocolVersion = 767;
    private readonly string _host;
    private readonly int _port;
    private readonly TimeSpan _timeout;

    public MinecraftPingStatusProvider()
        : this(ProductConstants.ServerHost, ProductConstants.ServerPort, TimeSpan.FromSeconds(5))
    {
    }

    public MinecraftPingStatusProvider(string host, int port, TimeSpan timeout)
    {
        _host = host;
        _port = port;
        _timeout = timeout;
    }

    public async Task<StatusModel> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_timeout);

            using var client = new TcpClient();
            var stopwatch = Stopwatch.StartNew();
            await client.ConnectAsync(_host, _port, timeout.Token);
            await using var stream = client.GetStream();

            await SendHandshakeAsync(stream, timeout.Token);
            await SendPacketAsync(stream, new byte[] { 0x00 }, timeout.Token);

            _ = await ReadVarIntAsync(stream, timeout.Token);
            var packetId = await ReadVarIntAsync(stream, timeout.Token);
            if (packetId != 0)
            {
                return StatusModel.Offline("서버 상태 응답 형식이 올바르지 않습니다.");
            }

            var jsonLength = await ReadVarIntAsync(stream, timeout.Token);
            var jsonBytes = await ReadExactAsync(stream, jsonLength, timeout.Token);
            stopwatch.Stop();

            return ParseStatusJson(Encoding.UTF8.GetString(jsonBytes), stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException)
        {
            return StatusModel.Offline("서버 상태를 확인하지 못했습니다.");
        }
        catch (Exception ex)
        {
            return StatusModel.Offline(ex.Message);
        }
    }

    public static StatusModel ParseStatusJson(string json, long latencyMs)
    {
        var node = JsonNode.Parse(json) ?? throw new JsonException("서버 상태 JSON이 비어 있습니다.");
        var description = node["description"];
        var sample = node["players"]?["sample"]?.AsArray()
            .OfType<JsonNode>()
            .Select(player => new PlayerSample(
                player["name"]?.GetValue<string>() ?? "-",
                player["id"]?.GetValue<string>()))
            .ToArray() ?? Array.Empty<PlayerSample>();

        return new StatusModel
        {
            State = ServerStatusState.Online,
            Host = ProductConstants.ServerHost,
            Port = ProductConstants.ServerPort,
            CurrentPlayers = node["players"]?["online"]?.GetValue<int>() ?? 0,
            MaxPlayers = node["players"]?["max"]?.GetValue<int>() ?? 0,
            Version = node["version"]?["name"]?.GetValue<string>() ?? "-",
            Motd = ReadMotd(description),
            LatencyMs = latencyMs,
            Players = sample
        };
    }

    private async Task SendHandshakeAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        using var payload = new MemoryStream();
        WriteVarInt(payload, 0);
        WriteVarInt(payload, ProtocolVersion);
        WriteString(payload, _host);
        var portBytes = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(portBytes, (ushort)_port);
        payload.Write(portBytes);
        WriteVarInt(payload, 1);
        await SendPacketAsync(stream, payload.ToArray(), cancellationToken);
    }

    private static async Task SendPacketAsync(NetworkStream stream, byte[] payload, CancellationToken cancellationToken)
    {
        using var packet = new MemoryStream();
        WriteVarInt(packet, payload.Length);
        packet.Write(payload, 0, payload.Length);
        await stream.WriteAsync(packet.ToArray(), cancellationToken);
    }

    private static async Task<int> ReadVarIntAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var numRead = 0;
        var result = 0;
        byte read;
        do
        {
            read = (await ReadExactAsync(stream, 1, cancellationToken))[0];
            var value = read & 0b01111111;
            result |= value << (7 * numRead);
            numRead++;
            if (numRead > 5)
            {
                throw new InvalidDataException("VarInt가 너무 깁니다.");
            }
        }
        while ((read & 0b10000000) != 0);

        return result;
    }

    private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int length, CancellationToken cancellationToken)
    {
        var buffer = new byte[length];
        var offset = 0;
        while (offset < length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, length - offset), cancellationToken);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            offset += read;
        }

        return buffer;
    }

    private static void WriteString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteVarInt(stream, bytes.Length);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteVarInt(Stream stream, int value)
    {
        while ((value & -128) != 0)
        {
            stream.WriteByte((byte)(value & 127 | 128));
            value = (int)((uint)value >> 7);
        }

        stream.WriteByte((byte)value);
    }

    private static string ReadMotd(JsonNode? node)
    {
        if (node is null)
        {
            return "-";
        }

        if (node is JsonValue value && value.TryGetValue<string>(out var text))
        {
            return text;
        }

        var builder = new StringBuilder();
        AppendText(node, builder);
        return builder.Length == 0 ? "-" : builder.ToString();
    }

    private static void AppendText(JsonNode? node, StringBuilder builder)
    {
        if (node is null)
        {
            return;
        }

        if (node["text"] is JsonValue textNode && textNode.TryGetValue<string>(out var text))
        {
            builder.Append(text);
        }

        if (node["extra"] is JsonArray extra)
        {
            foreach (var item in extra)
            {
                AppendText(item, builder);
            }
        }
    }
}
