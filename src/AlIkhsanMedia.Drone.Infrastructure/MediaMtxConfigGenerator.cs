using System.Text;
using AlIkhsanMedia.Drone.Core;

namespace AlIkhsanMedia.Drone.Infrastructure;

public static class MediaMtxConfigGenerator
{
    public static string Generate(EngineConfiguration config)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(config.StreamPath);
        if (config.Paths.Count is < 1 or > 6 || config.Paths.Distinct(StringComparer.Ordinal).Count() != config.Paths.Count) throw new ArgumentException("Daftar path stream harus unik dan berjumlah 1 sampai 6.", nameof(config));
        if (config.Paths.Any(path => string.IsNullOrWhiteSpace(path) || path.Any(static c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_')))) throw new ArgumentException("Path stream tidak valid.", nameof(config));
        var yaml = new StringBuilder().AppendLine("logLevel: info").AppendLine("logDestinations: [stdout]").AppendLine("logStructured: true")
            .AppendLine("readTimeout: 10s").AppendLine("writeTimeout: 10s").AppendLine("api: true").Append("apiAddress: ").AppendLine(config.ApiAddress)
            .AppendLine("metrics: true").Append("metricsAddress: ").AppendLine(config.MetricsAddress).AppendLine("pprof: false").AppendLine("playback: false")
            .AppendLine("rtsp: true").AppendLine("rtspTransports: [tcp]").Append("rtspAddress: ").AppendLine(config.RtspAddress)
            .AppendLine("rtmp: true").Append("rtmpAddress: ").AppendLine(config.RtmpAddress).AppendLine("hls: false").AppendLine("webrtc: true").Append("webrtcAddress: ").AppendLine(config.WebRtcAddress).AppendLine("srt: false")
            .AppendLine("pathDefaults:").AppendLine("  source: publisher").AppendLine("  overridePublisher: false").AppendLine("paths:");
        foreach (var path in config.Paths) yaml.Append("  ").Append(path).AppendLine(":").AppendLine("    source: publisher");
        return yaml.ToString();
    }
    public static async Task<string> WriteAtomicAsync(EngineConfiguration config, CancellationToken ct)
    {
        Directory.CreateDirectory(config.RuntimeDirectory); var destination = Path.Combine(config.RuntimeDirectory, "mediamtx.yml");
        var temporary = Path.Combine(config.RuntimeDirectory, $"mediamtx.{Guid.NewGuid():N}.tmp");
        await File.WriteAllTextAsync(temporary, Generate(config), new UTF8Encoding(false), ct).ConfigureAwait(false); File.Move(temporary, destination, true); return destination;
    }
}
