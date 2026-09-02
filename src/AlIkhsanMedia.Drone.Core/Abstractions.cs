using System.Net;

namespace AlIkhsanMedia.Drone.Core;

public interface IClock { DateTimeOffset UtcNow { get; } }
public sealed class SystemClock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }

public interface ISecretProtector
{
    ProtectedSecret Protect(string plaintext);
    string Unprotect(ProtectedSecret protectedSecret);
}

public interface IProcessContainmentService : IDisposable
{
    void Assign(System.Diagnostics.Process process);
}

public interface INetworkDiscoveryService
{
    Task<IReadOnlyList<NetworkCandidate>> DiscoverAsync(CancellationToken ct);
    IObservable<NetworkChange> Changes { get; }
}

public interface IPortInspectionService
{
    Task<PortInspection> InspectAsync(IPAddress address, int port, PortPurpose purpose, CancellationToken ct);
}

public interface IFirewallService
{
    Task<FirewallInspection> InspectAsync(AppPortPlan plan, CancellationToken ct);
    Task<FirewallRepairResult> RepairAsync(AppPortPlan plan, CancellationToken ct);
}

public sealed record NetworkChange(string AdapterId, IPAddress? PreviousAddress, IPAddress? CurrentAddress);
public enum PortPurpose { RtmpIngest, RtspOutput, SetupPortal }
public sealed record PortInspection(int Port, bool IsAvailable, int? OwnerProcessId, string? OwnerProcessName, string? DiagnosticCode);
public sealed record AppPortPlan(string ExecutablePath, int RtmpPort, int SetupPortalPort, bool IsPrivateProfile);
public sealed record FirewallInspection(bool RtmpRulePresent, bool PortalRulePresent, bool IsPrivateOnly, string? DiagnosticCode);
public sealed record FirewallRepairResult(bool Success, string? DiagnosticCode, string? OperatorMessage);
