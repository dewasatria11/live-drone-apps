using System.Net;
using System.Net.NetworkInformation;

namespace AlIkhsanMedia.Drone.Core;

public enum NetworkKind { Ethernet, WiFi, Hotspot, Vpn, Virtual, Unknown }
public enum NetworkProfile { Private, Public, DomainAuthenticated, Unknown }
public sealed record NetworkCandidate(string AdapterId, string FriendlyName, IPAddress Address, NetworkKind Kind, NetworkProfile Profile, bool HasDefaultGateway, bool UsesDhcp, OperationalStatus Status, int Score, bool IsRecommended, IReadOnlyList<string> Warnings);
public sealed record NetworkCandidateInput(string AdapterId, string FriendlyName, IPAddress Address, NetworkKind Kind, NetworkProfile Profile, bool HasDefaultGateway, bool UsesDhcp, OperationalStatus Status);

public static class NetworkCandidateScorer
{
    public static IReadOnlyList<NetworkCandidate> Score(IEnumerable<NetworkCandidateInput> inputs)
    {
        var candidates = inputs.Where(IsEligible).Select(Create).OrderByDescending(static x => x.Score).ThenBy(static x => x.FriendlyName, StringComparer.OrdinalIgnoreCase).ThenBy(static x => x.AdapterId, StringComparer.Ordinal).ToArray();
        if (candidates.Length == 0) return [];
        var bestScore = candidates[0].Score;
        return candidates.Select((candidate, index) => candidate with { IsRecommended = index == 0 && candidate.Score == bestScore }).ToArray();
    }
    public static NetworkKind Classify(string name, string description, NetworkInterfaceType type)
    {
        var text = $"{name} {description}".ToLowerInvariant();
        if (ContainsAny(text, "vpn", "tunnel", "wireguard", "tailscale", "zerotier", "openvpn")) return NetworkKind.Vpn;
        if (ContainsAny(text, "hyper-v", "vethernet", "wsl", "docker", "virtualbox", "vmware", "loopback")) return NetworkKind.Virtual;
        if (ContainsAny(text, "mobile hotspot", "wi-fi direct", "hosted network")) return NetworkKind.Hotspot;
        return type switch { NetworkInterfaceType.Wireless80211 => NetworkKind.WiFi, NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet or NetworkInterfaceType.FastEthernetFx or NetworkInterfaceType.FastEthernetT => NetworkKind.Ethernet, NetworkInterfaceType.Tunnel => NetworkKind.Vpn, _ => NetworkKind.Unknown };
    }
    private static bool IsEligible(NetworkCandidateInput input) => input.Status == OperationalStatus.Up && !IPAddress.IsLoopback(input.Address) && input.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
    private static NetworkCandidate Create(NetworkCandidateInput input)
    {
        var warnings = new List<string>(); var score = 0; var apipa = input.Address.GetAddressBytes() is [169, 254, _, _];
        if (input.HasDefaultGateway) score += 40; if (input.Kind is NetworkKind.WiFi or NetworkKind.Ethernet) score += 30;
        if (input.Profile == NetworkProfile.Private) score += 20; if (input.UsesDhcp && !apipa) score += 10;
        if (input.Kind == NetworkKind.Vpn) { score -= 50; warnings.Add("VPN tidak direkomendasikan untuk koneksi DJI Fly."); }
        if (input.Kind == NetworkKind.Virtual) { score -= 40; warnings.Add("Adapter virtual tidak direkomendasikan."); }
        if (apipa) { score -= 30; warnings.Add("Alamat APIPA menunjukkan jaringan belum memperoleh alamat dari router."); }
        if (!input.HasDefaultGateway) { score -= 20; warnings.Add("Adapter tidak mempunyai default gateway IPv4."); }
        if (input.Profile == NetworkProfile.Public) warnings.Add("Jaringan berstatus Public; akses HP mungkin diblokir firewall.");
        return new(input.AdapterId, input.FriendlyName, input.Address, input.Kind, input.Profile, input.HasDefaultGateway, input.UsesDhcp, input.Status, score, false, warnings);
    }
    private static bool ContainsAny(string text, params string[] terms) => terms.Any(text.Contains);
}
