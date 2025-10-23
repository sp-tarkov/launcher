using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SPTarkov.Core.Helpers;

public partial class WindowsClipboard(
    ILogger<WindowsClipboard> logger
)
{
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(IntPtr hWndNewOwner);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GlobalLock(IntPtr hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlock(IntPtr hMem);

    private const uint CfHdrop = 15;
    private const uint GmemMoveable = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct Dropfiles
    {
        public uint pFiles;
        public int ptX;
        public int ptY;
        public int fNC;
        public int fWide;
    }

    public void CopyFiles(string[] files)
    {
        var joined = string.Join("\0", files) + "\0\0";
        var data = Encoding.Unicode.GetBytes(joined);

        var dropFiles = new Dropfiles
        {
            pFiles = (uint) Marshal.SizeOf<Dropfiles>(),
            ptX = 0,
            ptY = 0,
            fNC = 0,
            fWide = 1
        };

        var totalSize = Marshal.SizeOf<Dropfiles>() + data.Length;

        var hGlobal = GlobalAlloc(GmemMoveable, (UIntPtr) totalSize);
        var ptr = GlobalLock(hGlobal);

        Marshal.StructureToPtr(dropFiles, ptr, false);
        var dataPtr = IntPtr.Add(ptr, Marshal.SizeOf<Dropfiles>());
        Marshal.Copy(data, 0, dataPtr, data.Length);

        if (!GlobalUnlock(hGlobal))
        {
            logger.LogError("Failed to unlock clipboard, error: {error}", Marshal.GetLastWin32Error());
        }

        if (!OpenClipboard(IntPtr.Zero))
        {
            logger.LogError("Failed to open clipboard, error: {error}", Marshal.GetLastWin32Error());
        }

        if (!EmptyClipboard())
        {
            logger.LogError("Failed to empty clipboard, error: {error}", Marshal.GetLastWin32Error());
        }
        _ = SetClipboardData(CfHdrop, hGlobal);

        if (!CloseClipboard())
        {
            logger.LogError("Failed to close clipboard, error: {error}", Marshal.GetLastWin32Error());
        }
    }
}
