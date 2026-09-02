namespace AlIkhsanMedia.Drone.Core;

public enum StreamState { Disabled, Waiting, Connecting, Live, Stale, Error }
public sealed record OperatorProblem(string DiagnosticCode, string Message, string RecoveryAction);
public sealed record StreamRuntimeState(StreamState State, DateTimeOffset? PublisherConnectedAt, DateTimeOffset? LastMediaAt, long BytesReceived, double? EstimatedBitrateKbps, IReadOnlyList<string> Codecs, int ReaderCount, OperatorProblem? Problem)
{
    public static StreamRuntimeState Disabled { get; } = new(StreamState.Disabled, null, null, 0, null, [], 0, null);
}

public sealed class StreamStateMachine(IClock clock, TimeSpan? staleAfter = null, TimeSpan? absentAfter = null)
{
    private readonly TimeSpan staleThreshold = staleAfter ?? TimeSpan.FromSeconds(3);
    private readonly TimeSpan absentThreshold = absentAfter ?? TimeSpan.FromSeconds(10);
    private DateTimeOffset? lastObservationAt; private long lastBytes;
    public StreamRuntimeState Current { get; private set; } = StreamRuntimeState.Disabled;

    public StreamRuntimeState Observe(bool enabled, bool publisherDetected, bool mediaReady, long bytesReceived, IReadOnlyList<string>? codecs = null, int readerCount = 0, OperatorProblem? error = null)
    {
        var now = clock.UtcNow;
        if (!enabled) return Set(StreamRuntimeState.Disabled, now, bytesReceived);
        if (error is not null) return Set(Current with { State = StreamState.Error, Problem = error }, now, bytesReceived);
        if (!publisherDetected)
        {
            if (Current.State is StreamState.Live or StreamState.Stale && Current.LastMediaAt is { } last && now - last < absentThreshold)
                return Set(Current with { State = StreamState.Stale, ReaderCount = readerCount, Problem = DiagnosticCatalog.Create(DiagnosticCodes.StreamStale) }, now, bytesReceived, false);
            return Set(new(StreamState.Waiting, null, null, 0, null, [], readerCount, null), now, bytesReceived);
        }
        var connectedAt = Current.PublisherConnectedAt ?? now;
        if (!mediaReady) return Set(new(StreamState.Connecting, connectedAt, Current.LastMediaAt, bytesReceived, null, codecs ?? [], readerCount, null), now, bytesReceived);
        var advanced = bytesReceived > lastBytes; var bitrate = CalculateBitrate(bytesReceived, now);
        if (advanced) return Set(new(StreamState.Live, connectedAt, now, bytesReceived, bitrate, codecs ?? [], readerCount, null), now, bytesReceived);
        var lastMedia = Current.LastMediaAt ?? now;
        var stale = now - lastMedia >= staleThreshold;
        return Set(new(stale ? StreamState.Stale : StreamState.Live, connectedAt, lastMedia, bytesReceived, bitrate, codecs ?? Current.Codecs, readerCount, stale ? DiagnosticCatalog.Create(DiagnosticCodes.StreamStale) : null), now, bytesReceived);
    }

    private double? CalculateBitrate(long bytes, DateTimeOffset now)
    {
        if (lastObservationAt is null || bytes < lastBytes) return null; var seconds = (now - lastObservationAt.Value).TotalSeconds;
        return seconds <= 0 ? Current.EstimatedBitrateKbps : Math.Round((bytes - lastBytes) * 8d / seconds / 1000d, 2);
    }
    private StreamRuntimeState Set(StreamRuntimeState state, DateTimeOffset now, long bytes, bool updateObservation = true)
    { Current = state; if (updateObservation) { lastObservationAt = now; lastBytes = bytes; } return state; }
}
