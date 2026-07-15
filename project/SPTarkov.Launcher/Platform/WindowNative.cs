using System.Runtime.InteropServices;

namespace SPTarkov.Launcher.Platform;

/// <summary>
/// The raw Win32 surface for controlling the launcher's own top-level window.
/// </summary>
internal static partial class WindowNative
{
    // ShowWindow command that hides the window and drops it from the taskbar.
    internal const int SW_HIDE = 0;

    // ShowWindow command that activates and displays the window, restoring it from a minimized state. Used to bring the window back from
    // the tray regardless of whether it was minimized when hidden.
    internal const int SW_RESTORE = 9;

    // Sets a window's show state.
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    // Brings the window to the foreground and gives it focus. Windows only lets a process do this in limited circumstances, which is what
    // AllowSetForegroundWindow below unblocks for the single-instance case.
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(IntPtr hWnd);

    // Passed to AllowSetForegroundWindow to permit *any* process to take the foreground on our behalf.
    internal const uint ASFW_ANY = 0xFFFFFFFF;

    // Lets a would-be second instance grant the running instance permission to take the foreground when it surfaces its window (Windows
    // otherwise blocks a background process from stealing focus).
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AllowSetForegroundWindow(uint dwProcessId);
}
