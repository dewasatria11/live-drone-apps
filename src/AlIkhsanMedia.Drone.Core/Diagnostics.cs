namespace AlIkhsanMedia.Drone.Core;

public sealed record DiagnosticItem(string Code, string Name, string Status, string Message, string RecoveryAction);
public sealed record DiagnosticsSnapshot(DateTimeOffset GeneratedAt, IReadOnlyList<DiagnosticItem> Items);
public sealed record SupportBundleInput(string SettingsJson, string DependencySummary, string DiagnosticSummary, IEnumerable<string> Logs);
