using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AlIkhsanMedia.Drone.Core;
namespace AlIkhsanMedia.Drone.Infrastructure;
public sealed class MediaMtxApiClient(HttpClient httpClient)
{
    public async Task<bool> IsHealthyAsync(CancellationToken ct) { using var response = await httpClient.GetAsync("v3/config/global/get", ct).ConfigureAwait(false); return response.IsSuccessStatusCode; }
    public async Task<IReadOnlyList<EnginePathSnapshot>> GetPathsAsync(CancellationToken ct)
    {
        var result = await httpClient.GetFromJsonAsync<PathListDto>("v3/paths/list", ct).ConfigureAwait(false);
        return result?.Items?.Select(static p => new EnginePathSnapshot(p.Name ?? "unknown", p.Ready, p.ReadyTime, p.BytesReceived, p.Tracks ?? [], p.Readers?.Length ?? 0)).ToArray() ?? [];
    }
    internal sealed record PathListDto([property: JsonPropertyName("items")] PathDto[]? Items);
    internal sealed record PathDto([property: JsonPropertyName("name")] string? Name, [property: JsonPropertyName("ready")] bool Ready, [property: JsonPropertyName("readyTime")] DateTimeOffset? ReadyTime, [property: JsonPropertyName("tracks")] string[]? Tracks, [property: JsonPropertyName("bytesReceived")] long BytesReceived, [property: JsonPropertyName("readers")] ReaderDto[]? Readers);
    internal sealed record ReaderDto([property: JsonPropertyName("id")] string? Id);
}
