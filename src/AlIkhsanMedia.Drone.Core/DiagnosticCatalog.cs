namespace AlIkhsanMedia.Drone.Core;
public static class DiagnosticCodes
{
    public const string EngineBinaryMissing = "ENG_BINARY_MISSING"; public const string EngineIntegrityFailed = "ENG_INTEGRITY_FAILED"; public const string EngineStartFailed = "ENG_START_FAILED"; public const string EngineCrashLoop = "ENG_CRASH_LOOP";
    public const string NetworkNoAdapter = "NET_NO_ADAPTER"; public const string NetworkIpChanged = "NET_IP_CHANGED"; public const string NetworkPublicProfile = "NET_PUBLIC_PROFILE"; public const string NetworkPossibleIsolation = "NET_POSSIBLE_ISOLATION";
    public const string PortRtmpInUse = "PORT_RTMP_IN_USE"; public const string PortRtspInUse = "PORT_RTSP_IN_USE"; public const string FirewallRuleMissing = "FW_RULE_MISSING";
    public const string StreamPublishRejected = "STR_PUBLISH_REJECTED"; public const string StreamStale = "STR_STALE"; public const string PreviewFailed = "PREVIEW_FAILED"; public const string VmixProbeFailed = "VMIX_PROBE_FAILED";
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal) { EngineBinaryMissing, EngineIntegrityFailed, EngineStartFailed, EngineCrashLoop, NetworkNoAdapter, NetworkIpChanged, NetworkPublicProfile, NetworkPossibleIsolation, PortRtmpInUse, PortRtspInUse, FirewallRuleMissing, StreamPublishRejected, StreamStale, PreviewFailed, VmixProbeFailed };
}
public static class DiagnosticCatalog
{
    public static OperatorProblem Create(string code) => code switch
    {
        DiagnosticCodes.StreamStale => new(code, "Video berhenti sementara.", "Periksa DJI Fly, sinyal drone, dan Wi-Fi."),
        DiagnosticCodes.NetworkPublicProfile => new(code, "Jaringan berstatus Public.", "Ubah jaringan ke Private atau gunakan router sendiri."),
        DiagnosticCodes.PortRtmpInUse => new(code, "Port RTMP sedang dipakai aplikasi lain.", "Tutup aplikasi pemilik port atau pilih port lain."),
        DiagnosticCodes.PortRtspInUse => new(code, "Port RTSP sedang dipakai aplikasi lain.", "Tutup aplikasi pemilik port atau pilih port lain."),
        _ when DiagnosticCodes.All.Contains(code) => new(code, "Penerima video perlu diperiksa.", "Buka Diagnostik untuk melihat tindakan berikutnya."),
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Diagnostic code tidak dikenal.")
    };
}
