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

    private sealed class TestClock(DateTimeOffset now) : IClock
    { public DateTimeOffset UtcNow { get; private set; } = now; public void Advance(TimeSpan value) => UtcNow += value; }
}
