using System.Net;
using System.IO;
using System.Text.Json;
using AlIkhsanMedia.Drone.Core;
using AlIkhsanMedia.Drone.Infrastructure;

namespace AlIkhsanMedia.Drone.App;

internal sealed class RuntimeSession : IAsyncDisposable
{
    private readonly MediaMtxService engine;
    public DashboardViewModel Dashboard { get; }
    public IReadOnlyList<StreamSlot> Slots { get; }

    private RuntimeSession(MediaMtxService engine, DashboardViewModel dashboard, IReadOnlyList<StreamSlot> slots) { this.engine = engine; Dashboard = dashboard; Slots = slots; }

    public static async Task<RuntimeSession> StartAsync(CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Aplikasi desktop hanya tersedia pada Windows 10/11 x64.");
        var protector = new DpapiSecretProtector(); var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlIkhsanMedia", "DroneVersion", "config", "settings.json");
        var loaded = await AtomicSettingsStore.LoadOrRecoverAsync(settingsPath, protector, new SystemClock(), ct).ConfigureAwait(false); await AtomicSettingsStore.SaveAsync(settingsPath, loaded.Settings, ct).ConfigureAwait(false);
        var candidates = await new WindowsNetworkDiscoveryService().DiscoverAsync(ct).ConfigureAwait(false); var candidate = candidates.FirstOrDefault(x => x.IsRecommended) ?? throw new InvalidOperationException("NET_NO_ADAPTER: Tidak ada adapter jaringan yang layak.");
        var slots = loaded.Settings.Slots.Select(s => new StreamSlot(new StreamSlotId(s.Id), s.DisplayName, s.Enabled, s.ProtectedStreamKey, StreamRuntimeState.Disabled)).ToArray(); var clearKeys = loaded.Settings.Slots.Select(s => protector.Unprotect(s.ProtectedStreamKey)).ToArray();
        var binary = Path.Combine(AppContext.BaseDirectory, "media", "mediamtx.exe"); var manifest = Path.Combine(AppContext.BaseDirectory, "media", "versions.json"); var expectedHash = ReadWindowsHash(manifest);
        var runtime = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlIkhsanMedia", "DroneVersion", "runtime"); var first = Array.FindIndex(loaded.Settings.Slots.ToArray(), x => x.Enabled); if (first < 0) first = 0;
        var path = clearKeys[first]; var ports = loaded.Settings.Ports; var config = new EngineConfiguration(binary, expectedHash, runtime, path, $"0.0.0.0:{ports.Rtmp}", $"127.0.0.1:{ports.Rtsp}", "127.0.0.1:9997", "127.0.0.1:9998");
        var service = new MediaMtxService(); var started = await service.StartAsync(config, ct).ConfigureAwait(false); if (!started.Success) { await service.DisposeAsync().ConfigureAwait(false); throw new InvalidOperationException(started.OperatorMessage ?? "Penerima video gagal dimulai."); }
        var slotModels = slots.Select((slot, i) => new DashboardSlotViewModel(slot, clearKeys[i], candidate.Address, ports.Rtmp, ports.Rtsp)); var dashboard = new DashboardViewModel(service, new WpfClipboardService(), slotModels); await dashboard.RefreshAsync(ct).ConfigureAwait(false); return new RuntimeSession(service, dashboard, slots);
    }

    private static string ReadWindowsHash(string manifest)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(manifest)); var artifacts = document.RootElement.GetProperty("dependencies")[0].GetProperty("artifacts"); foreach (var artifact in artifacts.EnumerateArray()) if (artifact.GetProperty("rid").GetString() == "win-x64") return artifact.GetProperty("executableSha256").GetString()!; throw new InvalidDataException("Manifest MediaMTX Windows x64 tidak mempunyai checksum executable.");
    }
    public async ValueTask DisposeAsync() => await engine.DisposeAsync().ConfigureAwait(false);
}

internal sealed class WpfClipboardService : IClipboardService
{
    public Task SetTextAsync(string text, CancellationToken ct) { System.Windows.Clipboard.SetText(text); return Task.CompletedTask; }
}
