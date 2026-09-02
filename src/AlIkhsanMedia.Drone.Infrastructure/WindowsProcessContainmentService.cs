using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AlIkhsanMedia.Drone.Core;
using Microsoft.Win32.SafeHandles;
namespace AlIkhsanMedia.Drone.Infrastructure;
public sealed class WindowsProcessContainmentService : IProcessContainmentService
{
    private readonly SafeFileHandle handle;
    public WindowsProcessContainmentService()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Job Object hanya tersedia pada target Windows.");
        handle = new SafeFileHandle(CreateJobObject(IntPtr.Zero, null), true); if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
        var info = new Extended { Basic = new Basic { LimitFlags = 0x00002000 } }; var length = Marshal.SizeOf<Extended>(); var pointer = Marshal.AllocHGlobal(length);
        try { Marshal.StructureToPtr(info, pointer, false); if (!SetInformationJobObject(handle.DangerousGetHandle(), 9, pointer, (uint)length)) throw new Win32Exception(Marshal.GetLastWin32Error()); }
        finally { Marshal.FreeHGlobal(pointer); }
    }
    public void Assign(Process process) { ArgumentNullException.ThrowIfNull(process); if (!AssignProcessToJobObject(handle.DangerousGetHandle(), process.Handle)) throw new Win32Exception(Marshal.GetLastWin32Error()); }
    public void Dispose() => handle.Dispose();
    [StructLayout(LayoutKind.Sequential)] private struct Io { public ulong A, B, C, D, E, F; }
    [StructLayout(LayoutKind.Sequential)] private struct Basic { public long A, B; public uint LimitFlags; public UIntPtr C, D; public uint E; public UIntPtr F; public uint G, H; }
    [StructLayout(LayoutKind.Sequential)] private struct Extended { public Basic Basic; public Io Io; public UIntPtr A, B, C, D; }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateJobObject(IntPtr attributes, string? name);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetInformationJobObject(IntPtr job, int infoClass, IntPtr info, uint length);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
}
