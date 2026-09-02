using System.Text.Json;
using System.Text.Json.Serialization;
using AlIkhsanMedia.Drone.Core;
namespace AlIkhsanMedia.Drone.Infrastructure;
public static class AtomicSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };
    public static async Task SaveAsync(string path, DroneSettings settings, CancellationToken ct)
    {
        var validation = SettingsValidator.Validate(settings); if (!validation.IsValid) throw new InvalidDataException(string.Join(" ", validation.Errors));
        var directory = Path.GetDirectoryName(path) ?? throw new ArgumentException("Path settings harus memiliki directory.", nameof(path)); Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous | FileOptions.WriteThrough))
            { await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, ct).ConfigureAwait(false); await stream.FlushAsync(ct).ConfigureAwait(false); }
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static async Task<DroneSettings> LoadAndMigrateAsync(string path, ISecretProtector protector, CancellationToken ct)
    { var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false); return SettingsMigration.MigrateToCurrent(json, protector); }

    public static async Task<SettingsLoadResult> LoadOrRecoverAsync(string path, ISecretProtector protector, IClock clock, CancellationToken ct)
    {
        if (!File.Exists(path)) return new(SettingsFactory.CreateDefault(protector), false, null);
        try { return new(await LoadAndMigrateAsync(path, protector, ct).ConfigureAwait(false), false, null); }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or FormatException)
        {
            var directory = Path.GetDirectoryName(path) ?? throw new ArgumentException("Path settings harus memiliki directory.", nameof(path));
            var backup = Path.Combine(directory, $"settings.corrupt.{clock.UtcNow:yyyyMMddTHHmmssfffZ}.json"); File.Move(path, backup, false);
            return new(SettingsFactory.CreateDefault(protector), true, backup);
        }
    }
}
