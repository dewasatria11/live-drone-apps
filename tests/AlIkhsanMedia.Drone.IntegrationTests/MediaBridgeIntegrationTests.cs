using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using AlIkhsanMedia.Drone.Core;
using AlIkhsanMedia.Drone.Infrastructure;

namespace AlIkhsanMedia.Drone.IntegrationTests;

public sealed class MediaBridgeIntegrationTests
{
    [Fact(Timeout = 90000)]
    public async Task RealRtmpToRtspReconnectCrashRecoveryAndCleanup()
    {
        var root = FindRepositoryRoot();
        var (relativeBinary, executableHash) = (OperatingSystem.IsWindows(), OperatingSystem.IsMacOS(), RuntimeInformation.ProcessArchitecture) switch
        {
            (true, _, Architecture.X64) => (Path.Combine("vendor", "mediamtx", "win-x64", "mediamtx.exe"), "114e6c0b514813658e10be55f8ab6eab950ae879943272a59b0a51d55930900a"),
            (_, true, Architecture.Arm64) => (Path.Combine("vendor", "mediamtx", "osx-arm64", "mediamtx"), "77fac2ea9b34fb8b402ec6580f25948df74917fdef69ad6f94fd2bccca239923"),
            _ => throw new PlatformNotSupportedException("Integration test memerlukan Windows x64 atau macOS arm64 artifact yang dipin.")
        };
        var binary = Path.Combine(root, relativeBinary);
        var runtime = Path.Combine(Path.GetTempPath(), $"al-ikhsan-media-{Guid.NewGuid():N}"); Directory.CreateDirectory(runtime);
        var ports = Enumerable.Range(0, 4).Select(_ => AllocatePort()).ToArray(); var path = SecureStreamKey.Create();
        var config = new EngineConfiguration(binary, executableHash, runtime, path,
            $"127.0.0.1:{ports[0]}", $"127.0.0.1:{ports[1]}", $"127.0.0.1:{ports[2]}", $"127.0.0.1:{ports[3]}");
        var rtmp = MediaUrlBuilder.BuildRtmp(IPAddress.Loopback, ports[0], path).AbsoluteUri; var rtsp = MediaUrlBuilder.BuildRtsp(ports[1], path).AbsoluteUri;
        await using var service = new MediaMtxService(); Process? publisher = null;
        try
        {
            var started = await service.StartAsync(config, default); Assert.True(started.Success, started.OperatorMessage);
            publisher = StartPublisher(rtmp); await WaitForPathAsync(service, path, default);
            var serviceProbe = await service.ProbeRtspAsync(new StreamSlotId(Guid.NewGuid()), default); Assert.True(serviceProbe.Success, serviceProbe.DiagnosticCode);
            var firstPackets = await ReadPacketsAsync(rtsp); Console.WriteLine($"RTSP packets initial={firstPackets}"); Assert.True(firstPackets > 0, "Reader RTSP pertama tidak menerima packet aktual.");

            KillAndDispose(publisher); publisher = null; await WaitForPathNotReadyAsync(service, path, default);
            publisher = StartPublisher(rtmp); await WaitForPathAsync(service, path, default);
            var reconnectPackets = await ReadPacketsAsync(rtsp); Console.WriteLine($"RTSP packets reconnect={reconnectPackets}"); Assert.True(reconnectPackets > 0, "Reader RTSP setelah reconnect tidak menerima packet.");

            var oldPid = service.OwnedProcessId; Assert.NotNull(oldPid); Process.GetProcessById(oldPid.Value).Kill(true);
            await WaitUntilAsync(async () => { var health = await service.GetHealthAsync(default); return health.State == EngineState.Ready && health.RestartCount == 1 && service.OwnedProcessId != oldPid; }, TimeSpan.FromSeconds(12));
            if (publisher.HasExited) { publisher.Dispose(); publisher = StartPublisher(rtmp); } else { KillAndDispose(publisher); publisher = StartPublisher(rtmp); }
            await WaitForPathAsync(service, path, default); var recoveredPackets = await ReadPacketsAsync(rtsp); Console.WriteLine($"RTSP packets recovered={recoveredPackets}; restartCount=1; oldPid={oldPid}; newPid={service.OwnedProcessId}"); Assert.True(recoveredPackets > 0, "Reader RTSP setelah recovery engine tidak menerima packet.");

            KillAndDispose(publisher); publisher = null; var ownedPid = service.OwnedProcessId; await service.StopAsync(default);
            Assert.Null(service.OwnedProcessId); if (ownedPid.HasValue) Assert.Throws<ArgumentException>(() => Process.GetProcessById(ownedPid.Value));
            foreach (var port in ports) AssertPortCanBind(port); Console.WriteLine($"Child PID {ownedPid} exited; all four ports rebound successfully.");
        }
        finally
        {
            if (publisher is not null) KillAndDispose(publisher);
            await service.StopAsync(default); Directory.Delete(runtime, true);
        }
    }

    private static Process StartPublisher(string url)
    {
        var info = new ProcessStartInfo("ffmpeg") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        foreach (var arg in new[] { "-hide_banner", "-loglevel", "error", "-re", "-f", "lavfi", "-i", "testsrc=size=320x180:rate=15", "-f", "lavfi", "-i", "sine=frequency=1000:sample_rate=44100", "-c:v", "libx264", "-preset", "ultrafast", "-tune", "zerolatency", "-pix_fmt", "yuv420p", "-g", "30", "-c:a", "aac", "-f", "flv", url }) info.ArgumentList.Add(arg);
        var process = Process.Start(info) ?? throw new InvalidOperationException("FFmpeg publisher gagal dimulai."); _ = process.StandardError.ReadToEndAsync(); return process;
    }

    private static async Task<long> ReadPacketsAsync(string url)
    {
        var info = new ProcessStartInfo("ffprobe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { "-v", "error", "-rtsp_transport", "tcp", "-read_intervals", "%+2", "-count_packets", "-show_entries", "stream=codec_name,nb_read_packets", "-of", "json", url }) info.ArgumentList.Add(arg);
        using var process = Process.Start(info) ?? throw new InvalidOperationException("FFprobe reader gagal dimulai."); var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8)); await process.WaitForExitAsync(timeout.Token); var json = await stdout; var error = await stderr; Assert.True(process.ExitCode == 0, error);
        using var document = JsonDocument.Parse(json); return document.RootElement.GetProperty("streams").EnumerateArray().Sum(static stream => stream.TryGetProperty("nb_read_packets", out var count) && long.TryParse(count.GetString(), out var value) ? value : 0);
    }

    private static Task WaitForPathAsync(MediaMtxService service, string path, CancellationToken ct) => WaitUntilAsync(async () => (await service.GetPathsAsync(ct)).Any(x => x.Name == path && x.Ready && x.BytesReceived > 0), TimeSpan.FromSeconds(10));
    private static Task WaitForPathNotReadyAsync(MediaMtxService service, string path, CancellationToken ct) => WaitUntilAsync(async () => !(await service.GetPathsAsync(ct)).Any(x => x.Name == path && x.Ready), TimeSpan.FromSeconds(10));
    private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    { using var cancellation = new CancellationTokenSource(timeout); while (!await condition()) await Task.Delay(100, cancellation.Token); }
    private static int AllocatePort() { using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); return ((IPEndPoint)listener.LocalEndpoint).Port; }
    private static void AssertPortCanBind(int port)
    {
        SocketException? last = null;
        for (var attempt = 0; attempt < 50; attempt++)
        {
            try { using var listener = new TcpListener(IPAddress.Loopback, port); listener.Start(); listener.Stop(); return; }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse) { last = ex; Thread.Sleep(100); }
        }
        throw new Xunit.Sdk.XunitException($"Port {port} tidak dapat digunakan kembali setelah shutdown: {last?.Message}");
    }
    private static void KillAndDispose(Process process) { if (!process.HasExited) { process.Kill(true); process.WaitForExit(3000); } process.Dispose(); }
    private static string FindRepositoryRoot() { var directory = new DirectoryInfo(AppContext.BaseDirectory); while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PRD_AL_IKHSAN_MEDIA_DRONE_VERSION.md"))) directory = directory.Parent; return directory?.FullName ?? throw new DirectoryNotFoundException("Root repository tidak ditemukan."); }
}
