using QRCoder;

namespace AlIkhsanMedia.Drone.SetupPortal;

public sealed class SetupQrCodeGenerator
{
    public static byte[] GeneratePng(Uri setupUri, int pixelsPerModule = 8)
    {
        ArgumentNullException.ThrowIfNull(setupUri);
        if (setupUri.Scheme is not ("http" or "https") || pixelsPerModule is < 2 or > 20) throw new ArgumentException("URL atau ukuran QR tidak valid.");
        using var generator = new QRCodeGenerator(); using var data = generator.CreateQrCode(setupUri.AbsoluteUri, QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(data).GetGraphic(pixelsPerModule);
    }
}
