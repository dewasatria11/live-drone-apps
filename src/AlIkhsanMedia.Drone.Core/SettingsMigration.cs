using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
namespace AlIkhsanMedia.Drone.Core;
public static class SettingsMigration
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };
    public static DroneSettings MigrateToCurrent(string json, ISecretProtector protector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json); ArgumentNullException.ThrowIfNull(protector);
        var root = JsonNode.Parse(json)?.AsObject() ?? throw new JsonException("Dokumen settings kosong."); var version = root["schemaVersion"]?.GetValue<int>() ?? 1;
        if (version is < 1 or > DroneSettings.CurrentSchemaVersion) throw new InvalidDataException($"Schema settings {version} tidak didukung.");
        if (version == DroneSettings.CurrentSchemaVersion)
        {
            var current = root.Deserialize<DroneSettings>(JsonOptions) ?? throw new JsonException("Settings tidak dapat dibaca."); EnsureValid(current); return current;
        }
        var slots = new List<StreamSlotSettings>();
        foreach (var item in root["slots"]?.AsArray() ?? [])
        {
            if (item is null) continue; var idText = item["id"]?.GetValue<string>(); var id = Guid.TryParse(idText, out var parsed) && parsed != Guid.Empty ? parsed : Guid.NewGuid();
            var name = item["displayName"]?.GetValue<string>() ?? $"Drone {slots.Count + 1}"; var enabled = item["enabled"]?.GetValue<bool>() ?? slots.Count == 0;
            var ciphertext = item["protectedStreamKey"]?.GetValue<string>(); var secret = string.IsNullOrWhiteSpace(ciphertext) ? protector.Protect(SecureStreamKey.Create()) : new ProtectedSecret("DPAPI-CurrentUser", ciphertext);
            slots.Add(new(id, name, enabled, secret));
        }
        while (slots.Count < 6) { var index = slots.Count + 1; slots.Add(new(Guid.NewGuid(), $"Drone {index}", index == 1 && slots.All(static x => !x.Enabled), protector.Protect(SecureStreamKey.Create()))); }
        if (slots.Count > 6) throw new InvalidDataException("Settings mempunyai lebih dari enam slot.");
        var migrated = new DroneSettings(2, new(ParseMode(root["network"]?["selectionMode"]?.GetValue<string>()), root["network"]?["adapterId"]?.GetValue<string>()),
            new(root["ports"]?["rtmp"]?.GetValue<int>() ?? 1935, root["ports"]?["rtsp"]?.GetValue<int>() ?? 8554, root["ports"]?["setupPortal"]?.GetValue<int>() ?? 8877),
            new(root["application"]?["minimizeToTray"]?.GetValue<bool>() ?? true, root["application"]?["launchAtStartup"]?.GetValue<bool>() ?? false, root["application"]?["previewEnabled"]?.GetValue<bool>() ?? true, root["application"]?["logRetentionDays"]?.GetValue<int>() ?? 7), slots);
        EnsureValid(migrated); return migrated;
    }
    private static NetworkSelectionMode ParseMode(string? value) => string.Equals(value, "specificAdapter", StringComparison.OrdinalIgnoreCase) ? NetworkSelectionMode.SpecificAdapter : NetworkSelectionMode.Automatic;
    private static void EnsureValid(DroneSettings settings) { var result = SettingsValidator.Validate(settings); if (!result.IsValid) throw new InvalidDataException(string.Join(" ", result.Errors)); }
}
