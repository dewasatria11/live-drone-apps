using System.Text.Json;
using AlIkhsanMedia.Drone.Core;

namespace AlIkhsanMedia.Drone.Infrastructure;

public sealed class DiagnosticsAggregator
{
    public static async Task<DiagnosticsSnapshot> CollectAsync(IMediaEngineService engine, IEnumerable<DiagnosticItem> environment, CancellationToken ct)
    {
        var items = new List<DiagnosticItem>(environment);
        var health = await engine.GetHealthAsync(ct).ConfigureAwait(false);
        items.Add(new("ENG_HEALTH", "Engine MediaMTX", health.State == EngineState.Ready ? "OK" : "ERROR", health.OperatorMessage ?? $"MediaMTX {health.Version}", "Buka Diagnostik dan periksa proses penerima."));
        foreach (var path in await engine.GetPathsAsync(ct).ConfigureAwait(false)) items.Add(new(path.Ready ? "STR_LIVE" : "STR_WAITING", $"Publisher {path.Name}", path.Ready ? "OK" : "WARN", $"Bytes diterima: {path.BytesReceived}; reader: {path.ReaderCount}", "Periksa DJI Fly dan jaringan lokal."));
        return new DiagnosticsSnapshot(DateTimeOffset.UtcNow, items);
    }
}

public static class SupportBundleWriter
{
    public static async Task WriteAsync(string path, SupportBundleInput input, CancellationToken ct)
    {
        var safeSettings = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(input.SettingsJson));
        safeSettings = System.Text.RegularExpressions.Regex.Replace(safeSettings, "(protectedStreamKey|streamKey|token|clipboard)\\\"\\s*:\\s*\\\"[^\\\"]*", "$1\\\":\\\"[REDACTED]");
        var logText = System.Text.RegularExpressions.Regex.Replace(string.Join(Environment.NewLine, input.Logs), @"(?i)rtmp://[^\s]+", "rtmp://[REDACTED]");
        var content = $"Al Ikhsan Media support bundle\nGenerated: {DateTimeOffset.UtcNow:O}\n\n[settings]\n{safeSettings}\n\n[dependencies]\n{input.DependencySummary}\n\n[diagnostics]\n{input.DiagnosticSummary}\n\n[logs]\n{logText}\n";
        var temp = path + ".tmp"; await File.WriteAllTextAsync(temp, content, ct).ConfigureAwait(false); File.Move(temp, path, true);
    }
}
