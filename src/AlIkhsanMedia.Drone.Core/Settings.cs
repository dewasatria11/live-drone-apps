namespace AlIkhsanMedia.Drone.Core;
public enum NetworkSelectionMode { Automatic, SpecificAdapter }
public sealed record NetworkSettings(NetworkSelectionMode SelectionMode, string? AdapterId);
public sealed record PortSettings(int Rtmp, int Rtsp, int SetupPortal);
public sealed record UserApplicationSettings(bool MinimizeToTray, bool LaunchAtStartup, bool PreviewEnabled, int LogRetentionDays);
public sealed record StreamSlotSettings(Guid Id, string DisplayName, bool Enabled, ProtectedSecret ProtectedStreamKey);
public sealed record DroneSettings(int SchemaVersion, NetworkSettings Network, PortSettings Ports, UserApplicationSettings Application, IReadOnlyList<StreamSlotSettings> Slots)
{
    public const int CurrentSchemaVersion = 2;
}
public sealed record SettingsValidationResult(bool IsValid, IReadOnlyList<string> Errors);
public sealed record SettingsLoadResult(DroneSettings Settings, bool RecoveredFromCorruption, string? BackupPath);
public static class SettingsFactory
{
    public static DroneSettings CreateDefault(ISecretProtector protector)
    {
        var slots = StreamSlotFactory.CreateSix(protector).Select(static x => new StreamSlotSettings(x.Id.Value, x.DisplayName, x.Enabled, x.StreamKey)).ToArray();
        return new(DroneSettings.CurrentSchemaVersion, new(NetworkSelectionMode.Automatic, null), new(1935, 8554, 8877), new(true, false, true, 7), slots);
    }
}
public static class SettingsValidator
{
    public static SettingsValidationResult Validate(DroneSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings); var errors = new List<string>();
        if (settings.SchemaVersion != DroneSettings.CurrentSchemaVersion) errors.Add("Versi schema settings tidak didukung.");
        if (settings.Slots.Count != StreamSlotFactory.SlotCount) errors.Add("Settings harus mempunyai tepat enam slot.");
        if (settings.Slots.Select(static x => x.Id).Distinct().Count() != settings.Slots.Count || settings.Slots.Any(static x => x.Id == Guid.Empty)) errors.Add("UUID slot harus unik dan tidak kosong.");
        if (settings.Slots.Any(static x => string.IsNullOrWhiteSpace(x.DisplayName))) errors.Add("Nama slot tidak boleh kosong.");
        if (settings.Slots.Any(static x => string.IsNullOrWhiteSpace(x.ProtectedStreamKey.Algorithm) || string.IsNullOrWhiteSpace(x.ProtectedStreamKey.Ciphertext))) errors.Add("Secret slot harus berupa envelope terenkripsi.");
        var ports = new[] { settings.Ports.Rtmp, settings.Ports.Rtsp, settings.Ports.SetupPortal };
        if (ports.Any(static x => x is < 1 or > 65535)) errors.Add("Port harus berada pada rentang 1–65535."); if (ports.Distinct().Count() != ports.Length) errors.Add("Port RTMP, RTSP, dan portal harus berbeda.");
        if (settings.Application.LogRetentionDays is not (3 or 7 or 14)) errors.Add("Retensi log harus 3, 7, atau 14 hari.");
        if (settings.Network.SelectionMode == NetworkSelectionMode.SpecificAdapter && string.IsNullOrWhiteSpace(settings.Network.AdapterId)) errors.Add("Adapter wajib dipilih pada mode SpecificAdapter.");
        return new(errors.Count == 0, errors);
    }
}
