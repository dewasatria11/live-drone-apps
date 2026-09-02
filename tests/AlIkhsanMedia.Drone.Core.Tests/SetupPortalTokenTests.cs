using AlIkhsanMedia.Drone.Core;
using AlIkhsanMedia.Drone.SetupPortal;

namespace AlIkhsanMedia.Drone.Core.Tests;

public sealed class SetupPortalTokenTests
{
    [Fact]
    public void TokenIsShortLivedSingleSlotAndRevocable()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero));
        var store = new SetupTokenStore(); var id = new StreamSlotId(Guid.NewGuid()); var link = store.Create(id, TimeSpan.FromMinutes(10), clock);
        Assert.InRange(link.Token.Length, 32, 64); Assert.True(store.TryGet(link.Token, clock, out var found)); Assert.Equal(id, found.SlotId);
        Assert.True(store.Revoke(link.Id)); Assert.False(store.TryGet(link.Token, clock, out _));
    }

    [Fact]
    public void ExpiredTokenIsRejected()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow); var store = new SetupTokenStore(); var link = store.Create(new StreamSlotId(Guid.NewGuid()), TimeSpan.FromMinutes(1), clock);
        clock.Advance(TimeSpan.FromMinutes(1)); Assert.False(store.TryGet(link.Token, clock, out _));
    }

    [Fact]
    public async Task PortalEscapesDataAndReturnsSecurityHeaders()
    {
        var clock = new TestClock(DateTimeOffset.UtcNow); var store = new SetupTokenStore(); var slot = new StreamSlotId(Guid.NewGuid()); var link = store.Create(slot, TimeSpan.FromMinutes(10), clock); var port = ReservePort();
        await using var portal = new SetupPortalService(store, clock);
        await portal.StartAsync(new SetupPortalConfiguration("127.0.0.1", port, "Al Ikhsan Media"), id => id == slot ? new SetupPortalData("Al Ikhsan <Media>", "Slot <1>", "rtmp://127.0.0.1:1935/key", clock.UtcNow.AddMinutes(10)) : null, default);
        using var client = new HttpClient(); var response = await client.GetAsync($"http://127.0.0.1:{port}/s/{link.Token}"); var html = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode); Assert.Equal("no-store, max-age=0", response.Headers.CacheControl?.ToString()); Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single()); Assert.Contains("Al Ikhsan &lt;Media&gt;", html); Assert.Contains("nonce=\"", html);
        var invalid = await client.GetAsync($"http://127.0.0.1:{port}/s/not-a-real-token"); Assert.Equal(System.Net.HttpStatusCode.NotFound, invalid.StatusCode);
    }

    [Fact]
    public void QrCodeContainsRealSetupPayload()
    {
        var uri = new Uri("http://192.168.1.20:8877/s/random-slot-token"); var png = SetupQrCodeGenerator.GeneratePng(uri);
        Assert.True(png.Length > 100); Assert.Equal(0x89, png[0]); Assert.Equal((byte)'P', png[1]);
    }

    private sealed class TestClock(DateTimeOffset now) : IClock
    { public DateTimeOffset UtcNow { get; private set; } = now; public void Advance(TimeSpan value) => UtcNow += value; }
    private static int ReservePort() { using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0); listener.Start(); return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port; }
}
