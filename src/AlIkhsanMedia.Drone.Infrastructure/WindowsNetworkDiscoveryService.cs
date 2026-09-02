using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Versioning;
using AlIkhsanMedia.Drone.Core;
using DomainNetworkChange = AlIkhsanMedia.Drone.Core.NetworkChange;
namespace AlIkhsanMedia.Drone.Infrastructure;
[SupportedOSPlatform("windows")]
public sealed class WindowsNetworkDiscoveryService : INetworkDiscoveryService, IObservable<DomainNetworkChange>, IDisposable
{
    private readonly ConcurrentDictionary<IObserver<DomainNetworkChange>, byte> observers = new(); private readonly object snapshotLock = new(); private readonly object debounceLock = new(); private Dictionary<string, System.Net.IPAddress> last = []; private CancellationTokenSource? debounce;
    public WindowsNetworkDiscoveryService()
    {
        EnsureWindows(); System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
    }
    public IObservable<DomainNetworkChange> Changes => this;
    public async Task<IReadOnlyList<NetworkCandidate>> DiscoverAsync(CancellationToken ct)
    {
        EnsureWindows(); var inputs = new List<NetworkCandidateInput>();
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            ct.ThrowIfCancellationRequested(); var properties = adapter.GetIPProperties(); var ipv4 = properties.UnicastAddresses.FirstOrDefault(static x => x.Address.AddressFamily == AddressFamily.InterNetwork)?.Address; if (ipv4 is null) continue;
            var gateway = properties.GatewayAddresses.Any(static x => x.Address.AddressFamily == AddressFamily.InterNetwork && !x.Address.Equals(System.Net.IPAddress.Any));
            var profile = await GetProfileAsync(properties.GetIPv4Properties()?.Index, ct).ConfigureAwait(false); var dhcp = properties.GetIPv4Properties()?.IsDhcpEnabled ?? false;
            inputs.Add(new(adapter.Id, adapter.Name, ipv4, NetworkCandidateScorer.Classify(adapter.Name, adapter.Description, adapter.NetworkInterfaceType), profile, gateway, dhcp, adapter.OperationalStatus));
        }
        var scored = NetworkCandidateScorer.Score(inputs); lock (snapshotLock) if (last.Count == 0) last = scored.ToDictionary(static x => x.AdapterId, static x => x.Address, StringComparer.Ordinal); return scored;
    }
    private static async Task<NetworkProfile> GetProfileAsync(int? index, CancellationToken ct)
    {
        if (index is null) return NetworkProfile.Unknown;
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add("-NoProfile"); start.ArgumentList.Add("-NonInteractive"); start.ArgumentList.Add("-Command"); start.ArgumentList.Add($"(Get-NetConnectionProfile -InterfaceIndex {index.Value} -ErrorAction SilentlyContinue).NetworkCategory");
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Windows network profile inspection gagal dimulai."); var output = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false); await process.WaitForExitAsync(ct).ConfigureAwait(false);
        return output.Trim() switch { "Private" => NetworkProfile.Private, "Public" => NetworkProfile.Public, "DomainAuthenticated" => NetworkProfile.DomainAuthenticated, _ => NetworkProfile.Unknown };
    }
    private void OnNetworkAddressChanged(object? sender, EventArgs args)
    {
        lock (debounceLock) { debounce?.Cancel(); debounce?.Dispose(); debounce = new CancellationTokenSource(); _ = DebounceChangesAsync(debounce.Token); }
    }
    private async Task DebounceChangesAsync(CancellationToken ct)
    { try { await Task.Delay(TimeSpan.FromMilliseconds(500), ct).ConfigureAwait(false); await PublishChangesAsync().ConfigureAwait(false); } catch (OperationCanceledException) when (ct.IsCancellationRequested) { } }
    private async Task PublishChangesAsync()
    {
        try
        {
            Dictionary<string, System.Net.IPAddress> previous; lock (snapshotLock) previous = new(last, StringComparer.Ordinal);
            var current = (await DiscoverAsync(default).ConfigureAwait(false)).ToDictionary(static x => x.AdapterId, static x => x.Address, StringComparer.Ordinal);
            foreach (var entry in previous) if (!current.TryGetValue(entry.Key, out var address) || !address.Equals(entry.Value)) foreach (var observer in observers.Keys) observer.OnNext(new(entry.Key, entry.Value, address));
            foreach (var entry in current) if (!previous.ContainsKey(entry.Key)) foreach (var observer in observers.Keys) observer.OnNext(new(entry.Key, null, entry.Value));
            lock (snapshotLock) last = current;
        }
        catch (Exception ex) { foreach (var observer in observers.Keys) observer.OnError(ex); }
    }
    public IDisposable Subscribe(IObserver<DomainNetworkChange> observer) { observers.TryAdd(observer, 0); return new Subscription(observers, observer); }
    public void Dispose() { System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged; lock (debounceLock) { debounce?.Cancel(); debounce?.Dispose(); debounce = null; } foreach (var observer in observers.Keys) observer.OnCompleted(); observers.Clear(); }
    private static void EnsureWindows() { if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows network discovery hanya tersedia pada target Windows."); }
    private sealed class Subscription(ConcurrentDictionary<IObserver<DomainNetworkChange>, byte> source, IObserver<DomainNetworkChange> observer) : IDisposable { public void Dispose() => source.TryRemove(observer, out _); }
}
