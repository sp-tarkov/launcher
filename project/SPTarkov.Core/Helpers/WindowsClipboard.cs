using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SPTarkov.Core.Helpers;

public partial class WindowsClipboard(ILogger<WindowsClipboard> logger)
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

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
    private static partial IntPtr GlobalFree(IntPtr hMem);

    private const uint CfHdrop = 15;
    private const uint GmemMoveable = 0x0002;
    private const uint CfUnicodetext = 13;

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
            pFiles = (uint)Marshal.SizeOf<Dropfiles>(),
            ptX = 0,
            ptY = 0,
            fNC = 0,
            fWide = 1,
        };

        var totalSize = Marshal.SizeOf<Dropfiles>() + data.Length;

        var hGlobal = GlobalAlloc(GmemMoveable, (UIntPtr)totalSize);
        if (hGlobal == IntPtr.Zero)
        {
            logger.LogError("GlobalAlloc failed, error: {error}", Marshal.GetLastWin32Error());
            return;
        }

        var ptr = GlobalLock(hGlobal);
        if (ptr == IntPtr.Zero)
        {
            logger.LogError("GlobalLock failed, error: {error}", Marshal.GetLastWin32Error());
            GlobalFree(hGlobal);
            return;
        }

        Marshal.StructureToPtr(dropFiles, ptr, false);
        var dataPtr = IntPtr.Add(ptr, Marshal.SizeOf<Dropfiles>());
        Marshal.Copy(data, 0, dataPtr, data.Length);

        // GlobalUnlock failure is only indicated by a non-zero GetLastError.
        if (!GlobalUnlock(hGlobal))
        {
            var error = Marshal.GetLastWin32Error();
            if (error != 0)
            {
                logger.LogError("Failed to unlock clipboard, error: {error}", error);
                GlobalFree(hGlobal);
                return;
            }
        }

        if (!OpenClipboard(IntPtr.Zero))
        {
            logger.LogError("Failed to open clipboard, error: {error}", Marshal.GetLastWin32Error());
            GlobalFree(hGlobal);
            return;
        }

        if (!EmptyClipboard())
        {
            logger.LogError("Failed to empty clipboard, error: {error}", Marshal.GetLastWin32Error());
            CloseClipboard();
            GlobalFree(hGlobal);
            return;
        }

        if (SetClipboardData(CfHdrop, hGlobal) == IntPtr.Zero)
        {
            logger.LogError("Failed to set clipboard data, error: {error}", Marshal.GetLastWin32Error());
            CloseClipboard();
            GlobalFree(hGlobal);
            return;
        }

        // On success, system owns hGlobal, so don't free it.
        if (!CloseClipboard())
        {
            logger.LogError("Failed to close clipboard, error: {error}", Marshal.GetLastWin32Error());
        }
    }

    public bool CopyText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            logger.LogWarning("CopyText called with empty string.");
            return false;
        }

        // Add a null terminator for Windows clipboard format
        var bytes = Encoding.Unicode.GetBytes(text + '\0');

        // Allocate global memory for the string
        var hGlobal = GlobalAlloc(GmemMoveable, (UIntPtr)bytes.Length);
        if (hGlobal == IntPtr.Zero)
        {
            logger.LogError("GlobalAlloc failed, error: {error}", Marshal.GetLastWin32Error());
            return false;
        }

        var ptr = GlobalLock(hGlobal);
        if (ptr == IntPtr.Zero)
        {
            logger.LogError("GlobalLock failed, error: {error}", Marshal.GetLastWin32Error());
            GlobalFree(hGlobal);
            return false;
        }

        // Copy the text into the allocated memory
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        GlobalUnlock(hGlobal);

        if (!OpenClipboard(IntPtr.Zero))
        {
            logger.LogError("Failed to open clipboard, error: {error}", Marshal.GetLastWin32Error());
            GlobalFree(hGlobal);
            return false;
        }

        if (!EmptyClipboard())
        {
            logger.LogError("Failed to empty clipboard, error: {error}", Marshal.GetLastWin32Error());
            CloseClipboard();
            GlobalFree(hGlobal);
            return false;
        }

        if (SetClipboardData(CfUnicodetext, hGlobal) == IntPtr.Zero)
        {
            logger.LogError("SetClipboardData failed, error: {error}", Marshal.GetLastWin32Error());
            CloseClipboard();
            GlobalFree(hGlobal);
            return false;
        }

        // On success, system owns hGlobal, so don't free it.
        if (!CloseClipboard())
        {
            logger.LogError("Failed to close clipboard, error: {error}", Marshal.GetLastWin32Error());
        }

        return true;
    }
}
