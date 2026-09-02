using System.Diagnostics;
using AlIkhsanMedia.Drone.Core;
namespace AlIkhsanMedia.Drone.Infrastructure;
public sealed class WindowsFirewallService : IFirewallService
{
    private const string RtmpRule = "Al Ikhsan Media Drone - RTMP In"; private const string PortalRule = "Al Ikhsan Media Drone - Setup Portal";
    public async Task<FirewallInspection> InspectAsync(AppPortPlan plan, CancellationToken ct)
    {
        EnsureWindows(); var rtmp = await RuleMatchesAsync(RtmpRule, plan.ExecutablePath, plan.RtmpPort, ct).ConfigureAwait(false); var portal = await RuleMatchesAsync(PortalRule, plan.ExecutablePath, plan.SetupPortalPort, ct).ConfigureAwait(false);
        return new(rtmp, portal, rtmp && portal, rtmp && portal ? null : DiagnosticCodes.FirewallRuleMissing);
    }
    public async Task<FirewallRepairResult> RepairAsync(AppPortPlan plan, CancellationToken ct)
    {
        EnsureWindows(); if (!plan.IsPrivateProfile) return new(false, DiagnosticCodes.NetworkPublicProfile, "Ubah jaringan ke Private sebelum membuka akses lokal.");
        var first = await AddRuleAsync(RtmpRule, plan.ExecutablePath, plan.RtmpPort, ct).ConfigureAwait(false); var second = first && await AddRuleAsync(PortalRule, plan.ExecutablePath, plan.SetupPortalPort, ct).ConfigureAwait(false);
        return second ? new(true, null, null) : new(false, DiagnosticCodes.FirewallRuleMissing, "Perbaikan firewall memerlukan izin administrator. Jalankan kembali tindakan Perbaiki Firewall.");
    }
    private static async Task<bool> RuleMatchesAsync(string name, string executable, int port, CancellationToken ct)
    {
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.Environment["AIM_RULE_NAME"] = name; start.Environment["AIM_RULE_PROGRAM"] = Path.GetFullPath(executable); start.Environment["AIM_RULE_PORT"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        start.ArgumentList.Add("-NoProfile"); start.ArgumentList.Add("-NonInteractive"); start.ArgumentList.Add("-Command");
        start.ArgumentList.Add("$r=Get-NetFirewallRule -DisplayName $env:AIM_RULE_NAME -ErrorAction SilentlyContinue | Where-Object {$_.Enabled -eq 'True' -and $_.Direction -eq 'Inbound' -and $_.Action -eq 'Allow' -and $_.Profile -eq 'Private'} | Select-Object -First 1; if($r){$p=$r|Get-NetFirewallPortFilter;$a=$r|Get-NetFirewallApplicationFilter; if($p.Protocol -eq 'TCP' -and $p.LocalPort -eq $env:AIM_RULE_PORT -and $a.Program -eq $env:AIM_RULE_PROGRAM){'true'}else{'false'}}else{'false'}");
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Windows Firewall inspection gagal dimulai."); var output = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false); var error = process.StandardError.ReadToEndAsync(ct); await process.WaitForExitAsync(ct).ConfigureAwait(false); await error.ConfigureAwait(false); return process.ExitCode == 0 && string.Equals(output.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }
    private static async Task<bool> AddRuleAsync(string name, string executable, int port, CancellationToken ct) => await RunNetshAsync(["advfirewall", "firewall", "add", "rule", $"name={name}", "dir=in", "action=allow", $"program={Path.GetFullPath(executable)}", "enable=yes", "profile=private", "protocol=TCP", $"localport={port}"], ct).ConfigureAwait(false) == 0;
    private static async Task<int> RunNetshAsync(IEnumerable<string> arguments, CancellationToken ct)
    {
        var start = new ProcessStartInfo("netsh.exe") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true }; foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Windows Firewall command gagal dimulai."); var stdout = process.StandardOutput.ReadToEndAsync(ct); var stderr = process.StandardError.ReadToEndAsync(ct); await process.WaitForExitAsync(ct).ConfigureAwait(false); await Task.WhenAll(stdout, stderr).ConfigureAwait(false); return process.ExitCode;
    }
    private static void EnsureWindows() { if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Firewall hanya tersedia pada target Windows."); }
}
