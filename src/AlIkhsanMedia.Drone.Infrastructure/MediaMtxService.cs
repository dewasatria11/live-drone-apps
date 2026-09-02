using System.Diagnostics;
using System.Threading.Channels;
using AlIkhsanMedia.Drone.Core;
namespace AlIkhsanMedia.Drone.Infrastructure;

public sealed class MediaMtxService : IMediaEngineService
{
    private readonly EngineRestartPolicy restartPolicy = new();
    private readonly SemaphoreSlim lifecycle = new(1, 1);
    private readonly IProcessContainmentService? containment;
    private readonly Channel<string> output = Channel.CreateBounded<string>(new BoundedChannelOptions(512) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = false, SingleWriter = false });
    private CancellationTokenSource? lifetime; private Process? process; private Task? monitor; private EngineConfiguration? config; private MediaMtxApiClient? api;
    private EngineState state = EngineState.Stopped; private DateTimeOffset startedAt; private int restartCount; private bool intentionalStop;
    public MediaMtxService(IProcessContainmentService? containment = null) => this.containment = containment ?? (OperatingSystem.IsWindows() ? new WindowsProcessContainmentService() : null);
    public int? OwnedProcessId => process is { HasExited: false } p ? p.Id : null;
    public IAsyncEnumerable<string> ReadOutputAsync(CancellationToken ct) => output.Reader.ReadAllAsync(ct);

    public async Task<StartEngineResult> StartAsync(EngineConfiguration config, CancellationToken ct)
    {
        await lifecycle.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (state is EngineState.Ready or EngineState.Starting or EngineState.Restarting) return new(true, null, null);
            state = EngineState.Starting; this.config = config; restartCount = 0; intentionalStop = false;
            await BinaryIntegrityVerifier.VerifyAsync(config.ExecutablePath, config.ExecutableSha256, ct).ConfigureAwait(false);
            await MediaMtxConfigGenerator.WriteAtomicAsync(config, ct).ConfigureAwait(false);
            lifetime = new CancellationTokenSource(); await StartProcessAndWaitAsync(lifetime.Token).ConfigureAwait(false);
            monitor = MonitorAsync(lifetime.Token); return new(true, null, null);
        }
        catch (FileNotFoundException) { state = EngineState.Faulted; return new(false, DiagnosticCodes.EngineBinaryMissing, "Penerima video tidak ditemukan. Instal ulang aplikasi dari paket resmi."); }
        catch (InvalidDataException) { state = EngineState.Faulted; return new(false, DiagnosticCodes.EngineIntegrityFailed, "Integritas penerima video gagal. Instal ulang aplikasi dari paket resmi."); }
        catch (Exception ex) when (ex is not OperationCanceledException) { state = EngineState.Faulted; return new(false, DiagnosticCodes.EngineStartFailed, $"Penerima video gagal dimulai: {ex.Message}"); }
        finally { lifecycle.Release(); }
    }

    private async Task StartProcessAndWaitAsync(CancellationToken ct)
    {
        var current = config ?? throw new InvalidOperationException("Konfigurasi engine belum tersedia.");
        var start = new ProcessStartInfo(current.ExecutablePath) { WorkingDirectory = current.RuntimeDirectory, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add(Path.Combine(current.RuntimeDirectory, "mediamtx.yml"));
        var child = new Process { StartInfo = start, EnableRaisingEvents = true }; if (!child.Start()) throw new InvalidOperationException("Process MediaMTX tidak dapat dimulai.");
        process = child; containment?.Assign(child); _ = DrainAsync(child.StandardOutput, ct); _ = DrainAsync(child.StandardError, ct);
        api = new MediaMtxApiClient(new HttpClient { BaseAddress = new Uri($"http://{current.ApiAddress}/"), Timeout = TimeSpan.FromSeconds(1) });
        using var readiness = CancellationTokenSource.CreateLinkedTokenSource(ct); readiness.CancelAfter(TimeSpan.FromSeconds(5));
        while (!readiness.IsCancellationRequested)
        {
            if (child.HasExited) throw new InvalidOperationException($"MediaMTX berhenti dengan kode {child.ExitCode}.");
            try { if (await api.IsHealthyAsync(readiness.Token).ConfigureAwait(false)) { state = EngineState.Ready; startedAt = DateTimeOffset.UtcNow; return; } } catch (HttpRequestException) { }
            await Task.Delay(100, readiness.Token).ConfigureAwait(false);
        }
        throw new TimeoutException("Health check MediaMTX melewati batas waktu.");
    }

    private async Task MonitorAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var watched = process; if (watched is null) return;
            try { await watched.WaitForExitAsync(ct).ConfigureAwait(false); } catch (OperationCanceledException) { return; }
            if (intentionalStop) return;
            if (!restartPolicy.TryGetDelay(restartCount, out var delay)) { state = EngineState.Faulted; return; }
            state = EngineState.Restarting; restartCount++;
            try { await Task.Delay(delay, ct).ConfigureAwait(false); await StartProcessAndWaitAsync(ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { return; } catch { if (restartCount >= restartPolicy.MaximumAttempts) state = EngineState.Faulted; }
        }
    }

    private async Task DrainAsync(StreamReader reader, CancellationToken ct)
    { try { while (!ct.IsCancellationRequested && await reader.ReadLineAsync(ct).ConfigureAwait(false) is { } line) output.Writer.TryWrite(line); } catch (OperationCanceledException) { } catch (ObjectDisposedException) { } }

    public async Task StopAsync(CancellationToken ct)
    {
        await lifecycle.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            intentionalStop = true; lifetime?.Cancel(); var child = process;
            if (child is { HasExited: false }) { try { child.Kill(false); } catch (InvalidOperationException) { } using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(3)); try { await child.WaitForExitAsync(timeout.Token).ConfigureAwait(false); } catch (OperationCanceledException) when (!ct.IsCancellationRequested) { child.Kill(true); await child.WaitForExitAsync(ct).ConfigureAwait(false); } }
            process?.Dispose(); process = null; state = EngineState.Stopped;
        }
        finally { lifecycle.Release(); }
        if (monitor is not null) try { await monitor.WaitAsync(ct).ConfigureAwait(false); } catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }
    }
    public async Task<MediaEngineHealth> GetHealthAsync(CancellationToken ct)
    {
        var healthy = false;
        if (state == EngineState.Ready && api is not null)
        {
            try { healthy = await api.IsHealthyAsync(ct).ConfigureAwait(false); }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested) { }
        }
        return new(healthy ? EngineState.Ready : state, "1.20.1", startedAt, restartCount, healthy ? null : "Penerima video perlu diperiksa.", healthy ? null : state == EngineState.Faulted ? DiagnosticCodes.EngineCrashLoop : DiagnosticCodes.EngineStartFailed);
    }
    public Task<IReadOnlyList<EnginePathSnapshot>> GetPathsAsync(CancellationToken ct) => api?.GetPathsAsync(ct) ?? Task.FromResult<IReadOnlyList<EnginePathSnapshot>>([]);
    public async Task<ProbeResult> ProbeRtspAsync(StreamSlotId slotId, CancellationToken ct)
    {
        _ = slotId;
        var current = config ?? throw new InvalidOperationException("Engine belum dikonfigurasi.");
        var parts = current.RtspAddress.Split(':');
        using var client = new System.Net.Sockets.TcpClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await client.ConnectAsync(parts[0], int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), timeout.Token).ConfigureAwait(false);
            await using var stream = client.GetStream(); var url = $"rtsp://{current.RtspAddress}/{current.StreamPath}";
            var request = System.Text.Encoding.ASCII.GetBytes($"DESCRIBE {url} RTSP/1.0\r\nCSeq: 1\r\nAccept: application/sdp\r\n\r\n");
            await stream.WriteAsync(request, timeout.Token).ConfigureAwait(false); var buffer = new byte[4096]; var read = await stream.ReadAsync(buffer, timeout.Token).ConfigureAwait(false);
            var response = System.Text.Encoding.ASCII.GetString(buffer, 0, read); return new(response.StartsWith("RTSP/1.0 200", StringComparison.Ordinal), read, response.StartsWith("RTSP/1.0 200", StringComparison.Ordinal) ? null : "VMIX_PROBE_FAILED");
        }
        catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or OperationCanceledException)
        { return new(false, 0, DiagnosticCodes.VmixProbeFailed); }
    }
    public async ValueTask DisposeAsync() { using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)); await StopAsync(timeout.Token).ConfigureAwait(false); lifetime?.Dispose(); containment?.Dispose(); lifecycle.Dispose(); }
}
