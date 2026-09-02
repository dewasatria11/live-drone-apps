using AlIkhsanMedia.Drone.Infrastructure;
using AlIkhsanMedia.Drone.Core;

namespace AlIkhsanMedia.Drone.Infrastructure.Tests;

public sealed class DiagnosticsAndSupportBundleTests
{
    [Fact]
    public async Task SupportBundleRedactsSecretsAndWritesAtomically()
    {
        var path = Path.Combine(Path.GetTempPath(), $"support-{Guid.NewGuid():N}.txt");
        try
        {
            await SupportBundleWriter.WriteAsync(path, new SupportBundleInput("{\"protectedStreamKey\":\"secret-key\",\"token\":\"portal-token\"}", "MediaMTX sha256: abc", "ENG_HEALTH OK", ["normal log", "rtmp://secret@example/key"]), default);
            var text = await File.ReadAllTextAsync(path); Assert.Contains("[REDACTED]", text); Assert.DoesNotContain("secret-key", text); Assert.DoesNotContain("portal-token", text); Assert.DoesNotContain("secret@example", text); Assert.Contains("MediaMTX sha256", text); Assert.False(File.Exists(path + ".tmp"));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
