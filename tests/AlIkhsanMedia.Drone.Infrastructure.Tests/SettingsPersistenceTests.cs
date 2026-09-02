using AlIkhsanMedia.Drone.Core;
namespace AlIkhsanMedia.Drone.Infrastructure.Tests;
public sealed class SettingsPersistenceTests
{
    [Fact] public async Task AtomicStoreRoundTripsValidatedCurrentSettingsAndLeavesNoTemporaryFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"settings-test-{Guid.NewGuid():N}"); var path = Path.Combine(directory, "settings.json"); var protector = new TestSecretProtector();
        var slots = StreamSlotFactory.CreateSix(protector).Select(static x => new StreamSlotSettings(x.Id.Value, x.DisplayName, x.Enabled, x.StreamKey)).ToArray();
        var settings = new DroneSettings(2, new(NetworkSelectionMode.Automatic, null), new(1935, 8554, 8877), new(true, false, true, 7), slots);
        try { await AtomicSettingsStore.SaveAsync(path, settings, default); var loaded = await AtomicSettingsStore.LoadAndMigrateAsync(path, protector, default); Assert.Equal(settings.SchemaVersion, loaded.SchemaVersion); Assert.Equal(settings.Network, loaded.Network); Assert.Equal(settings.Ports, loaded.Ports); Assert.Equal(settings.Application, loaded.Application); Assert.Equal(settings.Slots, loaded.Slots); Assert.Empty(Directory.GetFiles(directory, "*.tmp")); }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    [Fact] public async Task CorruptSettingsAreBackedUpAndSafeDefaultsReturned()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"settings-test-{Guid.NewGuid():N}"); Directory.CreateDirectory(directory); var path = Path.Combine(directory, "settings.json"); await File.WriteAllTextAsync(path, "{not-json");
        try { var result = await AtomicSettingsStore.LoadOrRecoverAsync(path, new TestSecretProtector(), new FixedClock(), default); Assert.True(result.RecoveredFromCorruption); Assert.NotNull(result.BackupPath); Assert.True(File.Exists(result.BackupPath)); Assert.False(File.Exists(path)); Assert.True(SettingsValidator.Validate(result.Settings).IsValid); }
        finally { Directory.Delete(directory, true); }
    }
    [Fact] public async Task InvalidSettingsAreNeverWritten()
    {
        var path = Path.Combine(Path.GetTempPath(), $"settings-test-{Guid.NewGuid():N}", "settings.json"); var invalid = new DroneSettings(2, new(NetworkSelectionMode.Automatic, null), new(0, 0, 0), new(true, false, true, 5), []);
        await Assert.ThrowsAsync<InvalidDataException>(() => AtomicSettingsStore.SaveAsync(path, invalid, default)); Assert.False(File.Exists(path));
    }
    private sealed class TestSecretProtector : ISecretProtector { public ProtectedSecret Protect(string plaintext) => new("test-only", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plaintext))); public string Unprotect(ProtectedSecret protectedSecret) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(protectedSecret.Ciphertext)); }
    private sealed class FixedClock : IClock { public DateTimeOffset UtcNow => new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero); }
}
