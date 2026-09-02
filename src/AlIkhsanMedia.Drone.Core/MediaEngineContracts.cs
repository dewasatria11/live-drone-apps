namespace AlIkhsanMedia.Drone.Core;

public readonly record struct StreamSlotId
{
    public Guid Value { get; }
    public StreamSlotId(Guid value) { if (value == Guid.Empty) throw new ArgumentException("UUID slot tidak boleh kosong.", nameof(value)); Value = value; }
}
public enum EngineState { Stopped, Starting, Ready, Restarting, Faulted }
public sealed record EngineConfiguration(string ExecutablePath, string ExecutableSha256, string RuntimeDirectory, string StreamPath, string RtmpAddress, string RtspAddress, string ApiAddress, string MetricsAddress, IReadOnlyList<string>? ActivePaths = null, string WebRtcAddress = "127.0.0.1:8889")
{
    public IReadOnlyList<string> Paths => ActivePaths is { Count: > 0 } ? ActivePaths : [StreamPath];
}
public sealed record StartEngineResult(bool Success, string? DiagnosticCode, string? OperatorMessage);
public sealed record MediaEngineHealth(EngineState State, string Version, DateTimeOffset StartedAt, int RestartCount, string? OperatorMessage, string? DiagnosticCode);
public sealed record EnginePathSnapshot(string Name, bool Ready, DateTimeOffset? ReadyAt, long BytesReceived, IReadOnlyList<string> Codecs, int ReaderCount);
public sealed record ProbeResult(bool Success, long PacketsRead, string? DiagnosticCode);
public interface IMediaEngineService : IAsyncDisposable
{
    Task<StartEngineResult> StartAsync(EngineConfiguration config, CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    Task<MediaEngineHealth> GetHealthAsync(CancellationToken ct);
    Task<IReadOnlyList<EnginePathSnapshot>> GetPathsAsync(CancellationToken ct);
    Task<ProbeResult> ProbeRtspAsync(StreamSlotId slotId, CancellationToken ct);
}
public interface IProcessProbe { Task<ProbeResult> ProbeAsync(string rtspUrl, TimeSpan timeout, CancellationToken ct); }
