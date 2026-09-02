using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using AlIkhsanMedia.Drone.Core;
namespace AlIkhsanMedia.Drone.Infrastructure;
public sealed class WindowsPortInspectionService : IPortInspectionService
{
    public async Task<PortInspection> InspectAsync(IPAddress address, int port, PortPurpose purpose, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Port owner inspection produksi hanya tersedia pada Windows."); if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        try { using var listener = new TcpListener(address, port); listener.Start(); listener.Stop(); return new(port, true, null, null, null); }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            var owner = await FindOwnerAsync(port, ct).ConfigureAwait(false); var code = purpose switch { PortPurpose.RtmpIngest => DiagnosticCodes.PortRtmpInUse, PortPurpose.RtspOutput => DiagnosticCodes.PortRtspInUse, _ => null }; return new(port, false, owner?.Id, owner?.Name, code);
        }
    }
    private static async Task<(int Id, string? Name)?> FindOwnerAsync(int port, CancellationToken ct)
    {
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add("-NoProfile"); start.ArgumentList.Add("-NonInteractive"); start.ArgumentList.Add("-Command"); start.ArgumentList.Add($"(Get-NetTCPConnection -State Listen -LocalPort {port} -ErrorAction SilentlyContinue | Select-Object -First 1).OwningProcess");
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Port owner inspection gagal dimulai."); var text = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false); await process.WaitForExitAsync(ct).ConfigureAwait(false);
        if (!int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var pid)) return null; try { using var owner = Process.GetProcessById(pid); return (pid, owner.ProcessName); } catch (ArgumentException) { return (pid, null); }
    }
}
