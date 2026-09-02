using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using AlIkhsanMedia.Drone.Core;
namespace AlIkhsanMedia.Drone.Infrastructure.Tests;
[SupportedOSPlatform("windows")]
public sealed class WindowsProductionAdapterTests
{
    [Fact] [Trait("Category", "Windows")]
    public void DpapiCurrentUserRoundTripsWithoutStoringPlaintext()
    {
        Assert.True(OperatingSystem.IsWindows(), "Test Windows wajib dijalankan pada Windows CI."); var protector = new DpapiSecretProtector(); var plaintext = SecureStreamKey.Create(); var envelope = protector.Protect(plaintext);
        Assert.Equal("DPAPI-CurrentUser", envelope.Algorithm); Assert.DoesNotContain(plaintext, envelope.Ciphertext, StringComparison.Ordinal); Assert.Equal(plaintext, protector.Unprotect(envelope));
    }
    [Fact] [Trait("Category", "Windows")]
    public async Task JobObjectKillsAssignedChildWhenClosed()
    {
        Assert.True(OperatingSystem.IsWindows(), "Test Windows wajib dijalankan pada Windows CI."); using var child = Process.Start(new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1 -t") { UseShellExecute = false, CreateNoWindow = true }) ?? throw new InvalidOperationException();
        using (var containment = new WindowsProcessContainmentService()) containment.Assign(child); using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)); await child.WaitForExitAsync(timeout.Token); Assert.True(child.HasExited);
    }
    [Fact] [Trait("Category", "Windows")]
    public async Task PortInspectionReportsOccupiedPortWithoutKillingOwner()
    {
        Assert.True(OperatingSystem.IsWindows(), "Test Windows wajib dijalankan pada Windows CI."); using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var result = await new WindowsPortInspectionService().InspectAsync(IPAddress.Loopback, port, PortPurpose.RtmpIngest, default); Assert.False(result.IsAvailable); Assert.Equal(DiagnosticCodes.PortRtmpInUse, result.DiagnosticCode); Assert.True(result.OwnerProcessId is null or > 0); Assert.NotNull(listener.LocalEndpoint);
    }
    [Fact] [Trait("Category", "Windows")]
    public async Task WindowsNetworkEnumerationReturnsOnlyEligibleIpv4Candidates()
    {
        Assert.True(OperatingSystem.IsWindows(), "Test Windows wajib dijalankan pada Windows CI."); using var service = new WindowsNetworkDiscoveryService(); var candidates = await service.DiscoverAsync(default);
        Assert.All(candidates, static candidate => { Assert.Equal(System.Net.NetworkInformation.OperationalStatus.Up, candidate.Status); Assert.Equal(AddressFamily.InterNetwork, candidate.Address.AddressFamily); Assert.False(IPAddress.IsLoopback(candidate.Address)); });
    }
    [Fact] [Trait("Category", "Windows")]
    public async Task FirewallInspectionIsReadOnlyAndReturnsStableDiagnosticWhenRulesAreAbsent()
    {
        Assert.True(OperatingSystem.IsWindows(), "Test Windows wajib dijalankan pada Windows CI."); var plan = new AppPortPlan(Environment.ProcessPath ?? "dotnet.exe", 51935, 58877, true);
        var result = await new WindowsFirewallService().InspectAsync(plan, default); Assert.Equal(result.RtmpRulePresent && result.PortalRulePresent, result.IsPrivateOnly); if (!result.IsPrivateOnly) Assert.Equal(DiagnosticCodes.FirewallRuleMissing, result.DiagnosticCode);
    }
}
