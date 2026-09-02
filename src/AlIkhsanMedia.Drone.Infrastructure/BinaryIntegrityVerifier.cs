using System.Security.Cryptography;
namespace AlIkhsanMedia.Drone.Infrastructure;
public static class BinaryIntegrityVerifier
{
    public static async Task VerifyAsync(string path, string expectedSha256, CancellationToken ct)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Binary MediaMTX tidak ditemukan. Instal ulang aplikasi dari paket resmi.", path);
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, true);
        var actual = await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false);
        if (!CryptographicOperations.FixedTimeEquals(actual, Convert.FromHexString(expectedSha256))) throw new InvalidDataException("Integritas MediaMTX gagal. Instal ulang aplikasi dari paket resmi.");
    }
}
