using System.Runtime.InteropServices;

namespace SPTarkov.Launcher.Platform;

/// <summary>
/// The raw Win32 surface for controlling the launcher's own top-level window.
/// </summary>
internal static partial class WindowNative
{
    /// <summary><see cref="ShowWindow"/> command that hides the window and drops it from the taskbar.</summary>
    internal const int SW_HIDE = 0;

    /// <summary><see cref="ShowWindow"/> command that activates and displays the window, restoring it from a minimized state.</summary>
    internal const int SW_RESTORE = 9;

    /// <summary>Sets a window's show state.</summary>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>Brings the window to the foreground and gives it focus; Windows only allows this in limited circumstances.</summary>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>Passed to <see cref="AllowSetForegroundWindow"/> to permit any process to take the foreground on our behalf.</summary>
    internal const uint ASFW_ANY = 0xFFFFFFFF;

    /// <summary>Grants the given process permission to take the foreground.</summary>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AllowSetForegroundWindow(uint dwProcessId);
}
