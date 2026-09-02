using System.Net;
using AlIkhsanMedia.Drone.Core;

namespace AlIkhsanMedia.Drone.Core.Tests;

public sealed class DashboardViewModelTests
{
    [Fact]
    public async Task RefreshMapsRealEnginePathAndBuildsSeparatedUrls()
    {
        var keys = Enumerable.Range(1, 6).Select(_ => SecureStreamKey.Create()).ToArray(); var slots = keys.Select((key, i) => new DashboardSlotViewModel(new StreamSlot(new StreamSlotId(Guid.NewGuid()), $"Drone {i + 1}", i == 0, new ProtectedSecret("test", "cipher"), StreamRuntimeState.Disabled), key, IPAddress.Parse("192.168.1.10"), 1935, 8554));
        var engine = new FakeEngine([new EnginePathSnapshot(keys[0], true, DateTimeOffset.UtcNow, 4096, ["h264"], 1)]); var clipboard = new RecordingClipboard(); var vm = new DashboardViewModel(engine, clipboard, slots);
        await vm.RefreshAsync(default);
        Assert.Equal("Siap", vm.EngineStatusText); Assert.Equal(StreamState.Live, vm.Slots[0].State); Assert.StartsWith("rtmp://192.168.1.10:1935/", vm.Slots[0].RtmpUrl.AbsoluteUri); Assert.StartsWith("rtsp://127.0.0.1:8554/", vm.Slots[0].RtspUrl.AbsoluteUri);
    }

    [Fact]
    public async Task CopyActionUsesClipboardAndSafeCloseBlocksLiveStop()
    {
        var slots = Enumerable.Range(1, 6).Select(i => new DashboardSlotViewModel(new StreamSlot(new StreamSlotId(Guid.NewGuid()), $"Drone {i}", true, new ProtectedSecret("test", "cipher"), StreamRuntimeState.Disabled), SecureStreamKey.Create(), IPAddress.Loopback, 1935, 8554));
        var clipboard = new RecordingClipboard(); var vm = new DashboardViewModel(new FakeEngine([]), clipboard, slots);
        await vm.CopyRtspAsync(vm.Slots[0], default); Assert.Equal(vm.Slots[0].RtspUrl.AbsoluteUri, clipboard.LastText); Assert.Equal(CloseDecision.Cancel, DashboardViewModel.ResolveClose(true, CloseDecision.StopAndExit)); Assert.Equal(CloseDecision.StopAndExit, DashboardViewModel.ResolveClose(false, CloseDecision.StopAndExit));
    }

    private sealed class RecordingClipboard : IClipboardService { public string? LastText { get; private set; } public Task SetTextAsync(string text, CancellationToken ct) { LastText = text; return Task.CompletedTask; } }
    private sealed class FakeEngine(IReadOnlyList<EnginePathSnapshot> paths) : IMediaEngineService
    {
        public Task<StartEngineResult> StartAsync(EngineConfiguration config, CancellationToken ct) => Task.FromResult(new StartEngineResult(true, null, null));
        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<MediaEngineHealth> GetHealthAsync(CancellationToken ct) => Task.FromResult(new MediaEngineHealth(EngineState.Ready, "test", DateTimeOffset.UtcNow, 0, null, null));
        public Task<IReadOnlyList<EnginePathSnapshot>> GetPathsAsync(CancellationToken ct) => Task.FromResult(paths);
        public Task<ProbeResult> ProbeRtspAsync(StreamSlotId slotId, CancellationToken ct) => Task.FromResult(new ProbeResult(true, 1, null));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
