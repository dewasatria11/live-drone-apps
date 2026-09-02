using System.Net;
namespace AlIkhsanMedia.Drone.Core;
public static class MediaUrlBuilder
{
    public static Uri BuildRtmp(IPAddress laptopAddress, int port, string streamKey)
    { Validate(laptopAddress, port, streamKey); return new Uri($"rtmp://{laptopAddress}:{port}/{streamKey}"); }
    public static Uri BuildRtsp(int port, string streamKey)
    { Validate(IPAddress.Loopback, port, streamKey); return new Uri($"rtsp://127.0.0.1:{port}/{streamKey}"); }
    private static void Validate(IPAddress address, int port, string key)
    {
        ArgumentNullException.ThrowIfNull(address); if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) throw new ArgumentException("v1.0 hanya mendukung alamat IPv4.", nameof(address));
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port)); if (!SecureStreamKey.IsValid(key)) throw new ArgumentException("Stream key tidak valid.", nameof(key));
    }
}
