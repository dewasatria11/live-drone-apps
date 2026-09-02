using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;

namespace AlIkhsanMedia.Drone.Core;

public interface IClipboardService { Task SetTextAsync(string text, CancellationToken ct); }
public enum CloseDecision { Cancel, KeepRunningInTray, StopAndExit }

public sealed class DashboardSlotViewModel : INotifyPropertyChanged
{
    private StreamRuntimeState runtime;
    private readonly string streamKey;
    internal string PathKey => streamKey;
    public StreamSlot Slot { get; }
    public string DisplayName => Slot.DisplayName;
    public StreamState State => runtime.State;
    public string StateText => State switch { StreamState.Waiting => "Menunggu video dari DJI Fly", StreamState.Connecting => "Menghubungkan video…", StreamState.Live => "Video masuk", StreamState.Stale => "Video berhenti sementara", StreamState.Error => "Penerima video perlu diperiksa", _ => "Tidak aktif" };
    public string? DiagnosticMessage => runtime.Problem?.Message;
    public string? RecoveryAction => runtime.Problem?.RecoveryAction;
    public long BytesReceived => runtime.BytesReceived;
    public double? EstimatedBitrateKbps => runtime.EstimatedBitrateKbps;
    public IReadOnlyList<string> Codecs => runtime.Codecs;
    public Uri RtmpUrl { get; }
    public Uri RtspUrl { get; }
    public bool IsLive => State == StreamState.Live;
    public DashboardSlotViewModel(StreamSlot slot, string streamKey, IPAddress address, int rtmpPort, int rtspPort) { Slot = slot; this.streamKey = streamKey; runtime = slot.Runtime; RtmpUrl = MediaUrlBuilder.BuildRtmp(address, rtmpPort, streamKey); RtspUrl = MediaUrlBuilder.BuildRtsp(rtspPort, streamKey); }
    public void Apply(EnginePathSnapshot? snapshot)
    {
        runtime = snapshot is null ? runtime with { State = StreamState.Waiting, Problem = null } : runtime with { State = snapshot.Ready ? StreamState.Live : StreamState.Connecting, BytesReceived = snapshot.BytesReceived, Codecs = snapshot.Codecs, ReaderCount = snapshot.ReaderCount, LastMediaAt = snapshot.ReadyAt };
        OnChanged(nameof(State)); OnChanged(nameof(StateText)); OnChanged(nameof(DiagnosticMessage)); OnChanged(nameof(RecoveryAction)); OnChanged(nameof(BytesReceived)); OnChanged(nameof(EstimatedBitrateKbps)); OnChanged(nameof(Codecs)); OnChanged(nameof(IsLive));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

public sealed class DashboardViewModel
{
    private readonly IMediaEngineService engine; private readonly IClipboardService clipboard;
    public IReadOnlyList<DashboardSlotViewModel> Slots { get; }
    public MediaEngineHealth? Health { get; private set; }
    public string EngineStatusText => Health?.State switch { EngineState.Ready => "Siap", EngineState.Restarting => "Memulihkan", EngineState.Faulted => "Perlu tindakan", _ => "Belum diperiksa" };
    public DashboardViewModel(IMediaEngineService engine, IClipboardService clipboard, IEnumerable<DashboardSlotViewModel> slots) { this.engine = engine; this.clipboard = clipboard; Slots = slots.Take(StreamSlotFactory.SlotCount).ToArray(); if (Slots.Count != StreamSlotFactory.SlotCount) throw new ArgumentException("Dashboard harus mempunyai enam slot.", nameof(slots)); }
    public async Task RefreshAsync(CancellationToken ct)
    {
        Health = await engine.GetHealthAsync(ct).ConfigureAwait(false); var paths = await engine.GetPathsAsync(ct).ConfigureAwait(false); foreach (var slot in Slots) slot.Apply(paths.FirstOrDefault(x => x.Name == slot.PathKey));
    }
    public Task CopyRtmpAsync(DashboardSlotViewModel slot, CancellationToken ct) => clipboard.SetTextAsync(slot.RtmpUrl.AbsoluteUri, ct);
    public Task CopyRtspAsync(DashboardSlotViewModel slot, CancellationToken ct) => clipboard.SetTextAsync(slot.RtspUrl.AbsoluteUri, ct);
    public static CloseDecision ResolveClose(bool anyLive, CloseDecision requested) => anyLive && requested == CloseDecision.StopAndExit ? CloseDecision.Cancel : requested;
}
