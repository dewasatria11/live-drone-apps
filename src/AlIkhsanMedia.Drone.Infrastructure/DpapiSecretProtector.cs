using System.Security.Cryptography;
using System.Text;
using System.Runtime.Versioning;
using AlIkhsanMedia.Drone.Core;
namespace AlIkhsanMedia.Drone.Infrastructure;
[SupportedOSPlatform("windows")]
public sealed class DpapiSecretProtector : ISecretProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("AlIkhsanMedia.Drone.v1");
    public ProtectedSecret Protect(string plaintext)
    {
        EnsureWindows(); ArgumentException.ThrowIfNullOrWhiteSpace(plaintext); var clear = Encoding.UTF8.GetBytes(plaintext);
        try { return new("DPAPI-CurrentUser", Convert.ToBase64String(ProtectedData.Protect(clear, Entropy, DataProtectionScope.CurrentUser))); }
        finally { CryptographicOperations.ZeroMemory(clear); }
    }
    public string Unprotect(ProtectedSecret protectedSecret)
    {
        EnsureWindows(); ArgumentNullException.ThrowIfNull(protectedSecret); if (protectedSecret.Algorithm != "DPAPI-CurrentUser") throw new InvalidDataException("Algoritma proteksi secret tidak didukung.");
        var cipher = Convert.FromBase64String(protectedSecret.Ciphertext); var clear = ProtectedData.Unprotect(cipher, Entropy, DataProtectionScope.CurrentUser);
        try { return Encoding.UTF8.GetString(clear); } finally { CryptographicOperations.ZeroMemory(clear); }
    }
    private static void EnsureWindows() { if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("DPAPI hanya tersedia pada target Windows."); }
}
