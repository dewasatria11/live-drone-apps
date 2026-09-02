using System.Net;
using System.Net.NetworkInformation;
namespace AlIkhsanMedia.Drone.Core.Tests;

public sealed class Phase2DomainTests
{
    private static readonly string[] ExpectedAdapterOrder = ["wifi", "vpn", "apipa", "virtual"];
    [Fact] public void FactoryCreatesExactlySixUniqueImmutableIdentitiesAndSecureKeys()
    {
        var protector = new TestSecretProtector(); var slots = StreamSlotFactory.CreateSix(protector);
        Assert.Equal(6, slots.Count); Assert.Equal(6, slots.Select(static x => x.Id).Distinct().Count()); Assert.All(slots, static slot => Assert.NotEqual(Guid.Empty, slot.Id.Value));
        Assert.True(slots[0].Enabled); Assert.All(slots.Skip(1), static slot => Assert.False(slot.Enabled));
        Assert.All(slots, slot => Assert.True(SecureStreamKey.IsValid(protector.Unprotect(slot.StreamKey))));
    }

    [Fact] public void GeneratedKeysHaveAtLeast128BitsAndDoNotRepeat()
    {
        var keys = Enumerable.Range(0, 1000).Select(_ => SecureStreamKey.Create()).ToArray(); Assert.Equal(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.All(keys, static key => { Assert.True(SecureStreamKey.IsValid(key)); Assert.DoesNotContain(key, SecureStreamKey.Redact(key), StringComparison.Ordinal); });
    }

    [Fact] public void UrlBuilderSeparatesLanRtmpFromLoopbackRtsp()
    {
        var key = SecureStreamKey.Create(); Assert.Equal($"rtmp://192.168.10.25:21935/{key}", MediaUrlBuilder.BuildRtmp(IPAddress.Parse("192.168.10.25"), 21935, key).AbsoluteUri.TrimEnd('/'));
        Assert.Equal($"rtsp://127.0.0.1:28554/{key}", MediaUrlBuilder.BuildRtsp(28554, key).AbsoluteUri.TrimEnd('/'));
    }

    [Fact] public void AdapterScoringIsDeterministicAndPenalizesVpnVirtualAndApipa()
    {
        var inputs = new[]
        {
            Input("vpn", "10.8.0.2", NetworkKind.Vpn, NetworkProfile.Private, true, true),
            Input("wifi", "192.168.1.20", NetworkKind.WiFi, NetworkProfile.Private, true, true),
            Input("virtual", "172.20.0.1", NetworkKind.Virtual, NetworkProfile.Private, false, false),
            Input("apipa", "169.254.1.2", NetworkKind.Ethernet, NetworkProfile.Public, false, false)
        };
        var scored = NetworkCandidateScorer.Score(inputs); Assert.Equal("wifi", scored[0].AdapterId); Assert.True(scored[0].IsRecommended); Assert.Equal(ExpectedAdapterOrder, scored.Select(static x => x.AdapterId));
        Assert.Contains(scored.Single(static x => x.AdapterId == "vpn").Warnings, static x => x.Contains("VPN", StringComparison.Ordinal)); Assert.Contains(scored.Single(static x => x.AdapterId == "apipa").Warnings, static x => x.Contains("APIPA", StringComparison.Ordinal));
    }

    [Theory] [InlineData("Tailscale Tunnel", NetworkInterfaceType.Ethernet, NetworkKind.Vpn)] [InlineData("vEthernet (WSL)", NetworkInterfaceType.Ethernet, NetworkKind.Virtual)] [InlineData("Intel Wi-Fi", NetworkInterfaceType.Wireless80211, NetworkKind.WiFi)] [InlineData("Realtek Ethernet", NetworkInterfaceType.GigabitEthernet, NetworkKind.Ethernet)]
    public void AdapterClassificationRecognizesNoise(string name, NetworkInterfaceType type, NetworkKind expected) => Assert.Equal(expected, NetworkCandidateScorer.Classify(name, name, type));

    [Fact] public void StateMachineUsesFakeClockForWaitingConnectingLiveStaleAndWaiting()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero)); var state = new StreamStateMachine(clock);
        Assert.Equal(StreamState.Waiting, state.Observe(true, false, false, 0).State); Assert.Equal(StreamState.Connecting, state.Observe(true, true, false, 0).State);
        clock.Advance(TimeSpan.FromSeconds(1)); var live = state.Observe(true, true, true, 100_000, ["H264"], 1); Assert.Equal(StreamState.Live, live.State); Assert.Equal(800, live.EstimatedBitrateKbps);
        clock.Advance(TimeSpan.FromSeconds(3)); Assert.Equal(StreamState.Stale, state.Observe(true, true, true, 100_000).State);
        clock.Advance(TimeSpan.FromSeconds(7)); Assert.Equal(StreamState.Waiting, state.Observe(true, false, false, 100_000).State);
    }

    [Fact] public void StaleStreamReturnsToLiveWhenBytesAdvance()
    {
        var clock = new TestClock(DateTimeOffset.UnixEpoch); var state = new StreamStateMachine(clock); state.Observe(true, true, true, 100); clock.Advance(TimeSpan.FromSeconds(3)); Assert.Equal(StreamState.Stale, state.Observe(true, true, true, 100).State);
        clock.Advance(TimeSpan.FromSeconds(1)); Assert.Equal(StreamState.Live, state.Observe(true, true, true, 200).State);
    }

    [Fact] public void SettingsMigrationPreservesValidV1IdentityAndCreatesMissingSlots()
    {
        var id = Guid.NewGuid(); var json = $$"""{"schemaVersion":1,"network":{"selectionMode":"automatic","adapterId":null},"ports":{"rtmp":1935,"rtsp":8554,"setupPortal":8877},"application":{"minimizeToTray":true,"launchAtStartup":false,"previewEnabled":true,"logRetentionDays":7},"slots":[{"id":"{{id}}","displayName":"Drone Utama","enabled":true,"protectedStreamKey":"legacy-cipher"}]}""";
        var migrated = SettingsMigration.MigrateToCurrent(json, new TestSecretProtector()); Assert.Equal(2, migrated.SchemaVersion); Assert.Equal(6, migrated.Slots.Count); Assert.Equal(id, migrated.Slots[0].Id); Assert.Equal("legacy-cipher", migrated.Slots[0].ProtectedStreamKey.Ciphertext); Assert.True(SettingsValidator.Validate(migrated).IsValid);
    }

    [Fact] public void SettingsValidatorRejectsDuplicatePortsAndIds()
    {
        var id = Guid.NewGuid(); var slots = Enumerable.Range(1, 6).Select(index => new StreamSlotSettings(index < 3 ? id : Guid.NewGuid(), $"Drone {index}", index == 1, new("test", "cipher"))).ToArray();
        var settings = new DroneSettings(2, new(NetworkSelectionMode.Automatic, null), new(1935, 1935, 8877), new(true, false, true, 7), slots); var result = SettingsValidator.Validate(settings);
        Assert.False(result.IsValid); Assert.Contains(result.Errors, static x => x.Contains("UUID", StringComparison.Ordinal)); Assert.Contains(result.Errors, static x => x.Contains("Port", StringComparison.Ordinal));
    }

    [Fact] public void DiagnosticCodesAreStableAndCatalogRejectsUnknownCode()
    { Assert.Equal(15, DiagnosticCodes.All.Count); Assert.Equal("STR_STALE", DiagnosticCatalog.Create(DiagnosticCodes.StreamStale).DiagnosticCode); Assert.Throws<ArgumentOutOfRangeException>(() => DiagnosticCatalog.Create("UNKNOWN")); }

    private static NetworkCandidateInput Input(string id, string address, NetworkKind kind, NetworkProfile profile, bool gateway, bool dhcp) => new(id, id, IPAddress.Parse(address), kind, profile, gateway, dhcp, OperationalStatus.Up);
    private sealed class TestClock(DateTimeOffset now) : IClock { public DateTimeOffset UtcNow { get; private set; } = now; public void Advance(TimeSpan value) => UtcNow += value; }
    private sealed class TestSecretProtector : ISecretProtector { public ProtectedSecret Protect(string plaintext) => new("test-only", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plaintext))); public string Unprotect(ProtectedSecret protectedSecret) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(protectedSecret.Ciphertext)); }
}
