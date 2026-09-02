using System.Net;
using System.IO;
using System.Text.Json;
using AlIkhsanMedia.Drone.Core;
using AlIkhsanMedia.Drone.Infrastructure;
using AlIkhsanMedia.Drone.SetupPortal;

namespace AlIkhsanMedia.Drone.App;

internal sealed class RuntimeSession : IAsyncDisposable
{
    private readonly MediaMtxService engine;
    private readonly SetupPortalService portal;
    public IReadOnlyDictionary<StreamSlotId, SetupLink> SetupLinks { get; }
    public int PortalPort { get; }
    private readonly string binaryPath; private readonly string binaryHash; private readonly IPAddress adapterAddress; private readonly PortSettings ports; private readonly string settingsJson;
    public DashboardViewModel Dashboard { get; }
    public IReadOnlyList<StreamSlot> Slots { get; }

    private RuntimeSession(MediaMtxService engine, SetupPortalService portal, DashboardViewModel dashboard, IReadOnlyList<StreamSlot> slots, IReadOnlyDictionary<StreamSlotId, SetupLink> links, int portalPort, string binaryPath, string binaryHash, IPAddress adapterAddress, PortSettings ports, string settingsJson) { this.engine = engine; this.portal = portal; Dashboard = dashboard; Slots = slots; SetupLinks = links; PortalPort = portalPort; this.binaryPath = binaryPath; this.binaryHash = binaryHash; this.adapterAddress = adapterAddress; this.ports = ports; this.settingsJson = settingsJson; }

    public static async Task<RuntimeSession> StartAsync(CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Aplikasi desktop hanya tersedia pada Windows 10/11 x64.");
        var protector = new DpapiSecretProtector(); var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlIkhsanMedia", "DroneVersion", "config", "settings.json");
        var loaded = await AtomicSettingsStore.LoadOrRecoverAsync(settingsPath, protector, new SystemClock(), ct).ConfigureAwait(false); await AtomicSettingsStore.SaveAsync(settingsPath, loaded.Settings, ct).ConfigureAwait(false);
        var candidates = await new WindowsNetworkDiscoveryService().DiscoverAsync(ct).ConfigureAwait(false); var candidate = candidates.FirstOrDefault(x => x.IsRecommended) ?? throw new InvalidOperationException("NET_NO_ADAPTER: Tidak ada adapter jaringan yang layak.");
        var slots = loaded.Settings.Slots.Select(s => new StreamSlot(new StreamSlotId(s.Id), s.DisplayName, s.Enabled, s.ProtectedStreamKey, StreamRuntimeState.Disabled)).ToArray(); var clearKeys = loaded.Settings.Slots.Select(s => protector.Unprotect(s.ProtectedStreamKey)).ToArray();
        var binary = Path.Combine(AppContext.BaseDirectory, "media", "mediamtx.exe"); var manifest = Path.Combine(AppContext.BaseDirectory, "media", "versions.json"); var expectedHash = ReadWindowsHash(manifest);
        var runtime = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlIkhsanMedia", "DroneVersion", "runtime"); var enabled = loaded.Settings.Slots.Select((slot, i) => (slot, i)).Where(x => x.slot.Enabled).ToArray(); var first = enabled.Length > 0 ? enabled[0].i : 0;
        var path = clearKeys[first]; var activePaths = enabled.Length > 0 ? enabled.Select(x => clearKeys[x.i]).ToArray() : [path]; var ports = loaded.Settings.Ports; var config = new EngineConfiguration(binary, expectedHash, runtime, path, $"0.0.0.0:{ports.Rtmp}", $"127.0.0.1:{ports.Rtsp}", "127.0.0.1:9997", "127.0.0.1:9998", activePaths);
        var service = new MediaMtxService(); var started = await service.StartAsync(config, ct).ConfigureAwait(false); if (!started.Success) { await service.DisposeAsync().ConfigureAwait(false); throw new InvalidOperationException(started.OperatorMessage ?? "Penerima video gagal dimulai."); }
        var tokenStore = new SetupTokenStore(); var links = slots.Select(slot => (slot.Id, Link: tokenStore.Create(slot.Id, TimeSpan.FromMinutes(10), new SystemClock()))).ToDictionary(x => x.Id, x => x.Link); var portal = new SetupPortalService(tokenStore, new SystemClock());
        await portal.StartAsync(new SetupPortalConfiguration(candidate.Address.ToString(), ports.SetupPortal, "Al Ikhsan Media (Drone Version)"), id => { var index = slots.ToList().FindIndex(x => x.Id == id); return index < 0 ? null : new SetupPortalData("Al Ikhsan Media (Drone Version)", slots[index].DisplayName, MediaUrlBuilder.BuildRtmp(candidate.Address, ports.Rtmp, clearKeys[index]).AbsoluteUri, DateTimeOffset.UtcNow.AddMinutes(10)); }, ct).ConfigureAwait(false);
        var slotModels = slots.Select((slot, i) => new DashboardSlotViewModel(slot, clearKeys[i], candidate.Address, ports.Rtmp, ports.Rtsp)); var dashboard = new DashboardViewModel(service, new WpfClipboardService(), slotModels); await dashboard.RefreshAsync(ct).ConfigureAwait(false); return new RuntimeSession(service, portal, dashboard, slots, links, ports.SetupPortal, binary, expectedHash, candidate.Address, ports, JsonSerializer.Serialize(loaded.Settings));
    }

    public async Task<DiagnosticsSnapshot> CollectDiagnosticsAsync(CancellationToken ct)
    {
        var firewall = await new WindowsFirewallService().InspectAsync(new AppPortPlan(binaryPath, ports.Rtmp, ports.SetupPortal, true), ct);
        var env = new[] { new DiagnosticItem("DEP_MEDIAMTX", "MediaMTX checksum", "OK", binaryHash, "Tidak ada tindakan."), new DiagnosticItem("NET_ADAPTER", "Adapter/IP", "OK", adapterAddress.ToString(), "Gunakan adapter jaringan yang sama."), new DiagnosticItem("PORTS", "Port RTMP/RTSP/Portal", "OK", $"{ports.Rtmp}/{ports.Rtsp}/{ports.SetupPortal}", "Periksa port jika bentrok."), new DiagnosticItem("FIREWALL", "Windows Firewall", firewall.IsPrivateOnly ? "OK" : "WARN", firewall.IsPrivateOnly ? "Rule Private aktif" : "Rule aplikasi belum lengkap", "Jalankan Perbaiki Firewall atau gunakan instruksi manual.") };
        return await DiagnosticsAggregator.CollectAsync(engine, env, ct);
    }
    public Task<FirewallRepairResult> RepairFirewallAsync(CancellationToken ct) => new WindowsFirewallService().RepairAsync(new AppPortPlan(binaryPath, ports.Rtmp, ports.SetupPortal, true), ct);
    public async Task<string> CreateSupportPreviewAsync(CancellationToken ct) { var d = await CollectDiagnosticsAsync(ct); return string.Join(Environment.NewLine, d.Items.Select(x => $"[{x.Status}] {x.Name}: {x.Message}\nTindakan: {x.RecoveryAction}")); }
    public async Task ExportSupportBundleAsync(string path, CancellationToken ct) { var d = await CollectDiagnosticsAsync(ct); await SupportBundleWriter.WriteAsync(path, new SupportBundleInput(settingsJson, $"MediaMTX SHA-256: {binaryHash}", string.Join("\n", d.Items.Select(x => $"{x.Code}: {x.Status} - {x.Message}")), []), ct); }

    private static string ReadWindowsHash(string manifest)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(manifest)); var artifacts = document.RootElement.GetProperty("dependencies")[0].GetProperty("artifacts"); foreach (var artifact in artifacts.EnumerateArray()) if (artifact.GetProperty("rid").GetString() == "win-x64") return artifact.GetProperty("executableSha256").GetString()!; throw new InvalidDataException("Manifest MediaMTX Windows x64 tidak mempunyai checksum executable.");
    }
    public async ValueTask DisposeAsync() { await portal.DisposeAsync().ConfigureAwait(false); await engine.DisposeAsync().ConfigureAwait(false); }
}

internal sealed class WpfClipboardService : IClipboardService
{
    public Task SetTextAsync(string text, CancellationToken ct) { System.Windows.Clipboard.SetText(text); return Task.CompletedTask; }
}
