using System.Text;
using AlIkhsanMedia.Drone.Core;

namespace AlIkhsanMedia.Drone.Infrastructure;

public static class MediaMtxConfigGenerator
{
    public static string Generate(EngineConfiguration config)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(config.StreamPath);
        if (config.StreamPath.Any(static c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))) throw new ArgumentException("Path stream tidak valid.", nameof(config));
        return new StringBuilder().AppendLine("logLevel: info").AppendLine("logDestinations: [stdout]").AppendLine("logStructured: true")
            .AppendLine("readTimeout: 10s").AppendLine("writeTimeout: 10s").AppendLine("api: true").Append("apiAddress: ").AppendLine(config.ApiAddress)
            .AppendLine("metrics: true").Append("metricsAddress: ").AppendLine(config.MetricsAddress).AppendLine("pprof: false").AppendLine("playback: false")
            .AppendLine("rtsp: true").AppendLine("rtspTransports: [tcp]").Append("rtspAddress: ").AppendLine(config.RtspAddress)
            .AppendLine("rtmp: true").Append("rtmpAddress: ").AppendLine(config.RtmpAddress).AppendLine("hls: false").AppendLine("webrtc: false").AppendLine("srt: false")
            .AppendLine("pathDefaults:").AppendLine("  source: publisher").AppendLine("  overridePublisher: false").AppendLine("paths:")
            .Append("  ").Append(config.StreamPath).AppendLine(":").AppendLine("    source: publisher").ToString();
    }
    public static async Task<string> WriteAtomicAsync(EngineConfiguration config, CancellationToken ct)
    {
        Directory.CreateDirectory(config.RuntimeDirectory); var destination = Path.Combine(config.RuntimeDirectory, "mediamtx.yml");
        var temporary = Path.Combine(config.RuntimeDirectory, $"mediamtx.{Guid.NewGuid():N}.tmp");
        await File.WriteAllTextAsync(temporary, Generate(config), new UTF8Encoding(false), ct).ConfigureAwait(false); File.Move(temporary, destination, true); return destination;
    }
}
