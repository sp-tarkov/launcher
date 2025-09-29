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

    private const uint CF_HDROP = 15;
    private const uint GMEM_MOVEABLE = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct DROPFILES
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

        var dropFiles = new DROPFILES
        {
            pFiles = (uint) Marshal.SizeOf<DROPFILES>(),
            ptX = 0,
            ptY = 0,
            fNC = 0,
            fWide = 1
        };

        var totalSize = Marshal.SizeOf<DROPFILES>() + data.Length;

        var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr) totalSize);
        var ptr = GlobalLock(hGlobal);

        Marshal.StructureToPtr(dropFiles, ptr, false);
        var dataPtr = IntPtr.Add(ptr, Marshal.SizeOf<DROPFILES>());
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
        SetClipboardData(CF_HDROP, hGlobal);

        if (!CloseClipboard())
        {
            logger.LogError("Failed to close clipboard, error: {error}", Marshal.GetLastWin32Error());
        }
    }
}
