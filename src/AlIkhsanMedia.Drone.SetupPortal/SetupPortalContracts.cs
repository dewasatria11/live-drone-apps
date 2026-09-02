using System.Collections.Concurrent;
using System.Security.Cryptography;
using AlIkhsanMedia.Drone.Core;

namespace AlIkhsanMedia.Drone.SetupPortal;

public sealed record SetupLinkId(Guid Value);
public sealed record SetupLink(SetupLinkId Id, string Token, StreamSlotId SlotId, DateTimeOffset ExpiresAt);
public sealed record SetupPortalConfiguration(string BindAddress, int Port, string ProductName);
public sealed record SetupPortalData(string ProductName, string SlotName, string RtmpUrl, DateTimeOffset ExpiresAt);

public sealed class SetupTokenStore
{
    private readonly ConcurrentDictionary<string, SetupLink> links = new(StringComparer.Ordinal);
    public SetupLink Create(StreamSlotId slotId, TimeSpan lifetime, IClock clock)
    {
        if (lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromMinutes(10)) throw new ArgumentOutOfRangeException(nameof(lifetime));
        Span<byte> bytes = stackalloc byte[24]; RandomNumberGenerator.Fill(bytes);
        var token = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var link = new SetupLink(new SetupLinkId(Guid.NewGuid()), token, slotId, clock.UtcNow.Add(lifetime)); links[token] = link; return link;
    }
    public bool TryGet(string token, IClock clock, out SetupLink link)
    {
        if (links.TryGetValue(token, out link!) && link.ExpiresAt > clock.UtcNow) return true;
        if (link is not null) links.TryRemove(token, out _); link = null!; return false;
    }
    public bool Revoke(SetupLinkId id) => links.Any(pair => pair.Value.Id == id && links.TryRemove(pair.Key, out _));
}
