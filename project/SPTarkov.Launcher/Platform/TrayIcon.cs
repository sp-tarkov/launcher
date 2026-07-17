using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace SPTarkov.Launcher.Platform;

/// <summary>
/// A native Windows notification-area (system tray) icon.
/// </summary>
/// <remarks>
/// <para>.NET has no built-in tray API, so we have to talk to the shell directly. <c>Shell_NotifyIcon</c> adds and removes the icon, and
/// <c>TrackPopupMenuEx</c> shows the native context menu. There is no MudBlazor or Photino involvement here.</para>
///
/// <para>The core problem here is that every tray icon needs an active window. Windows reports every click on the icon as a window message,
/// so the icon has to be attached to a window that can receive those messages. We do not want a visible window, so we create a message-only
/// window; it never appears on screen or in the taskbar and exists purely as a message target.</para>
///
/// <para>Messages require a message loop, on a thread we own. A window only receives messages while a thread works its queue with a
/// <c>GetMessage</c> and <c>DispatchMessage</c> loop. We cannot borrow the Photino thread for that, so this class starts a dedicated
/// background STA thread, <c>"SptTrayIcon"</c>, that creates the window and runs the loop for the icon's lifetime. Because of this, menu
/// clicks fire on that tray thread; any callback that touches the UI must marshal back onto the Photino thread itself.
/// <c>Helpers.TrayHelper</c>'s callbacks handle that.</para>
///
/// <para>The window "belongs" to the tray thread, so every icon/menu operation has to run there. Other threads never call the Win32
/// functions directly. Instead, they post a custom message with <c>PostMessage</c>, and the loop picks it up and does work on the correct
/// thread. <c>Show</c>, <c>Hide</c>, and <c>Dispose</c> are the only thread-safe members.</para>
/// </remarks>
internal sealed class TrayIcon : IDisposable
{
    /// <summary>
    /// Entry of the context menu. The same struct models all three menu shapes: a normal clickable item, a separator line, a submenu.
    /// </summary>
    internal readonly record struct MenuItem(string? Label, Action? OnClick, IReadOnlyList<MenuItem>? Children = null)
    {
        internal static MenuItem Separator
        {
            get { return new MenuItem(null, null); }
        }

        /// <summary>A labelled item that opens a nested menu of children.</summary>
        internal static MenuItem Submenu(string label, IReadOnlyList<MenuItem> children)
        {
            return new MenuItem(label, null, children);
        }

        internal bool IsSeparator
        {
            get { return Label is null; }
        }

        /// <summary><c>true</c> when this entry opens a nested menu rather than firing an action.</summary>
        internal bool IsSubmenu
        {
            get { return Children is { Count: > 0 }; }
        }
    }

    // Names our window class, the template a window is created from, and identifies the icon we register in the tray.
    private const string WindowClassName = "SptLauncherTrayIconWindow";
    private const uint TrayIconId = 1;

    // Our private messages, based off WM_APP (the id range Windows reserves for application use). Other threads post these to the tray
    // window to request work be done on the tray thread.
    private const uint WM_APP = 0x8000;
    private const uint WM_TRAYCALLBACK = WM_APP + 1; // shell to us - the user interacted with the icon
    private const uint WM_TRAY_REFRESH = WM_APP + 2; // us to tray thread - re-sync the icon to _visible
    private const uint WM_TRAY_QUIT = WM_APP + 3; // us to tray thread - remove the icon and shut down

    // Windows message ids we react to (taken from the Win32 headers).
    private const uint WM_NULL = 0x0000; // used to nudge the message queue
    private const uint WM_DESTROY = 0x0002; // window is being destroyed
    private const uint WM_RBUTTONUP = 0x0205; // right mouse button released
    private const uint WM_LBUTTONUP = 0x0202; // left mouse button released
    private const uint WM_LBUTTONDBLCLK = 0x0203; // left mouse button double-clicked
    private const uint WM_CONTEXTMENU = 0x007B; // context-menu request (inc keyboard navigation)

    // Shell_NotifyIcon commands.
    private const uint NIM_ADD = 0x0; // add the icon to the tray
    private const uint NIM_DELETE = 0x2; // remove it

    // NOTIFYICONDATA flags that tell the shell which of the struct's optional fields are filled.
    private const uint NIF_MESSAGE = 0x1; // uCallbackMessage is set (route interactions to our window)
    private const uint NIF_ICON = 0x2; // hIcon is set
    private const uint NIF_TIP = 0x4; // tooltip text is set

    // Popup-menu flags. How each AppendMenu entry behaves, and how TrackPopupMenuEx reports the result.
    private const uint MF_STRING = 0x0; // a normal text item
    private const uint MF_SEPARATOR = 0x800; // a divider line
    private const uint MF_POPUP = 0x10; // the item opens a submenu (its id is a submenu handle)
    private const uint TPM_RETURNCMD = 0x100; // make TrackPopupMenuEx return the chosen id instead of posting it
    private const uint TPM_RIGHTBUTTON = 0x2; // let the right mouse button select items too

    // LoadImage parameters for pulling the .ico off disk at the system's small-icon size.
    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x10;
    private const int SM_CXSMICON = 49; // recommended width
    private const int SM_CYSMICON = 50; // recommended height

    // Parent handle that makes CreateWindowEx produce a message-only (invisible) window.
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private readonly string _iconPath;
    private readonly string _tooltip;
    private readonly Func<IReadOnlyList<MenuItem>> _menuProvider; // called every right-click to build a fresh menu
    private readonly Action? _onActivate; // run when the icon is left-clicked to restore the app
    private readonly ILogger _logger;

    // Signalled once the tray thread has finished trying to create the window, so Dispose knows the window handle has settled (either
    // valid, or zero on failure) before it posts the quit message.
    private readonly ManualResetEventSlim _ready = new(false);
    private Thread? _thread;
    private WndProcDelegate? _wndProc; // kept alive for the window's lifetime so the GC can't collect it
    private IntPtr _hwnd; // the message-only window handle
    private IntPtr _hIcon; // the loaded icon handle
    private bool _iconAdded; // whether Shell_NotifyIcon(NIM_ADD) is currently in effect; touched on the tray thread only
    private bool _started; // whether the tray thread has been launched
    private volatile bool _visible; // desired visibility; written by any thread, read by the tray thread

    internal TrayIcon(string iconPath, string tooltip, Func<IReadOnlyList<MenuItem>> menuProvider, Action? onActivate, ILogger logger)
    {
        _iconPath = iconPath;
        _tooltip = tooltip;
        _menuProvider = menuProvider;
        _onActivate = onActivate;
        _logger = logger;
    }

    /// <summary>Shows the tray icon, starting the message-loop thread on first use. Never blocks caller.</summary>
    internal void Show()
    {
        _visible = true;

        // First call launches the thread; ThreadProc reads _visible and adds the icon itself.
        if (StartIfNeeded())
        {
            return;
        }

        // Already running. Ask the tray thread to update status of the icon with the new _visible value.
        if (_hwnd != IntPtr.Zero)
        {
            PostMessage(_hwnd, WM_TRAY_REFRESH, IntPtr.Zero, IntPtr.Zero);
        }
    }

    /// <summary>Hides the tray icon. The thread and window stay alive for reuse. Thread-safe.</summary>
    internal void Hide()
    {
        _visible = false;
        if (_started && _hwnd != IntPtr.Zero)
        {
            PostMessage(_hwnd, WM_TRAY_REFRESH, IntPtr.Zero, IntPtr.Zero);
        }
    }

    /// <summary>Removes the icon and tears down the window and thread. Called from main thread at shutdown.</summary>
    public void Dispose()
    {
        if (!_started)
        {
            return;
        }

        // Only Dispose waits for the window to exist, so it can post the quit message reliably.
        _ready.Wait(TimeSpan.FromSeconds(2));
        if (_hwnd != IntPtr.Zero)
        {
            PostMessage(_hwnd, WM_TRAY_QUIT, IntPtr.Zero, IntPtr.Zero);
        }

        // Give the loop a moment to unwind (remove the icon, destroy the window, exit) before letting go.
        _thread?.Join(TimeSpan.FromSeconds(2));
        _ready.Dispose();
    }

    // Launch the tray thread on first use. Returns true only the first time, when it actually starts the thread, so callers know the thread
    // will pick up the initial _visible state itself.
    private bool StartIfNeeded()
    {
        if (_started)
        {
            return false;
        }

        _started = true;
        _thread = new Thread(ThreadProc) { IsBackground = true, Name = "SptTrayIcon" };

        // The tray window lives on this thread, and the shell APIs that drive it require their owning thread to be a single-threaded
        // apartment. A new thread defaults to MTA, so switch to STA before starting it.
        if (OperatingSystem.IsWindows()) // Silence CA1416
        {
            _thread.SetApartmentState(ApartmentState.STA);
        }

        _thread.Start();
        return true;
    }

    // The lifecycle of the tray icon:
    // 1. register a window class and create the hidden message-only window
    // 2. load the icon file
    // 3. pump the message loop until WM_TRAY_QUIT tears the window down
    // 4. clean up
    private void ThreadProc()
    {
        try
        {
            // Handed to native code as a raw function pointer, so store it in a field first to keep the GC from collecting while the window
            // can still call back in.
            _wndProc = WndProc;
            var hInstance = GetModuleHandle(null);

            // The "window class" template that windows are created from.
            var windowClass = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                hInstance = hInstance,
                lpszClassName = WindowClassName,
            };
            RegisterClassEx(ref windowClass);

            // Set HWND_MESSAGE as the parent, making a message-only window.
            _hwnd = CreateWindowEx(0, WindowClassName, string.Empty, 0, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, hInstance, IntPtr.Zero);

            // Load the .ico from disk at the OS small-icon size.
            var size = GetSystemMetrics(SM_CXSMICON);
            _hIcon = LoadImage(IntPtr.Zero, _iconPath, IMAGE_ICON, size, GetSystemMetrics(SM_CYSMICON), LR_LOADFROMFILE);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to initialize tray window: {ex}", ex);
        }
        finally
        {
            // Unblock Dispose regardless of success. At this point _hwnd is either a real handle or still zero.
            _ready.Set();
        }

        // If the window could not be created there is nothing to pump.
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        // Apply the requested visibility.
        SyncIcon();

        // Message loop. GetMessage blocks until a message arrives, then DispatchMessage routes it to WndProc. It returns 0 (ending the
        // loop) once WndProc calls PostQuitMessage during teardown.
        while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        // We are in shut down. Release the icon handle and drop the class registration.
        if (_hIcon != IntPtr.Zero)
        {
            DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }

        UnregisterClass(WindowClassName, GetModuleHandle(null));
    }

    // The window's message handler. DispatchMessage invokes this on the tray thread for every message the window receives.
    private IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            // Another thread changed _visible via Show() or Hide().
            case WM_TRAY_REFRESH:
                SyncIcon();
                return IntPtr.Zero;

            // Dispose asked us to shut down.
            case WM_TRAY_QUIT:
                RemoveIcon();
                DestroyWindow(hwnd); // Raises WM_DESTROY below
                return IntPtr.Zero;

            // The window is going away so end the message loop.
            case WM_DESTROY:
                PostQuitMessage(0);
                return IntPtr.Zero;

            // The shell reporting an interaction with our icon.
            case WM_TRAYCALLBACK:
                HandleTrayInteraction((uint)(lParam.ToInt64() & 0xFFFF)); // low word of lParam is the mouse message
                return IntPtr.Zero;
        }

        // Anything we do not explicitly handle passes to the default handler.
        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    // Turns the raw mouse message from the tray callback into an action.
    private void HandleTrayInteraction(uint mouseMessage)
    {
        switch (mouseMessage)
        {
            // Right-click or keyboard interaction opens the menu.
            case WM_RBUTTONUP:
            case WM_CONTEXTMENU:
                ShowMenu();
                break;

            // Restore the launcher on a single or double left-click.
            case WM_LBUTTONUP:
            case WM_LBUTTONDBLCLK:
                SafeInvoke(_onActivate);
                break;
        }
    }

    // Builds and shows the right-click menu and handles user actions.
    private void ShowMenu()
    {
        IReadOnlyList<MenuItem> items;
        try
        {
            // Built fresh on each open so the menu reflects live app state.
            items = _menuProvider();
        }
        catch (Exception ex)
        {
            _logger.LogError("Tray menu provider failed: {ex}", ex);
            return;
        }

        if (items.Count == 0)
        {
            return;
        }

        // Win32 menus identify the chosen item by an integer "command id". BuildMenu assigns clickable leaves 1-based ids in the order they
        // are added and stores their actions in this list, so the id TrackPopupMenuEx returns indexes back to the action to run.
        var actions = new List<Action?>();
        var menu = BuildMenu(items, actions);
        if (menu == IntPtr.Zero)
        {
            return;
        }

        try
        {
            GetCursorPos(out var cursor);

            // SetForegroundWindow and the following WM_NULL message is the workaround pattern to correctly dismiss the menu then a user
            // clicks off of the menu, anywhere else.
            WindowNative.SetForegroundWindow(_hwnd);

            // TPM_RETURNCMD blocks until the user picks an item (or dismisses the menu) and returns the chosen command id.
            var command = TrackPopupMenuEx(menu, TPM_RETURNCMD | TPM_RIGHTBUTTON, cursor.X, cursor.Y, _hwnd, IntPtr.Zero);

            PostMessage(_hwnd, WM_NULL, IntPtr.Zero, IntPtr.Zero);

            // Command of 0 means nothing was picked.
            if (command > 0 && command <= actions.Count)
            {
                SafeInvoke(actions[command - 1]);
            }
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    // Builds a menu, recursing into submenus. Each clickable item's action is appended to actions so its 1-based position in that list
    // becomes its menu command id.
    private IntPtr BuildMenu(IReadOnlyList<MenuItem> items, List<Action?> actions)
    {
        var menu = CreatePopupMenu();
        if (menu == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        foreach (var item in items)
        {
            if (item.IsSeparator)
            {
                AppendMenu(menu, MF_SEPARATOR, 0, null);
            }
            else if (item.IsSubmenu)
            {
                // A submenu is itself a menu handle. MF_POPUP tells AppendMenu the "id" argument is that handle rather than a command id.
                var submenu = BuildMenu(item.Children!, actions);
                AppendMenuSub(menu, MF_STRING | MF_POPUP, submenu, item.Label);
            }
            else
            {
                // Reserve the next command id (its 1-based index) and record the matching action.
                actions.Add(item.OnClick);
                AppendMenu(menu, MF_STRING, actions.Count, item.Label);
            }
        }

        return menu;
    }

    // Bring the icon in line with the desired visibility. Runs on the tray thread.
    private void SyncIcon()
    {
        if (_visible)
        {
            AddIcon();
        }
        else
        {
            RemoveIcon();
        }
    }

    // Adds the icon to the tray.
    private void AddIcon()
    {
        if (_iconAdded)
        {
            return;
        }

        // NIF_MESSAGE wires interactions back to WM_TRAYCALLBACK and NIF_ICON/NIF_TIP supply image and tooltip.
        var data = CreateIconData(NIF_MESSAGE | NIF_ICON | NIF_TIP);

        if (Shell_NotifyIcon(NIM_ADD, ref data))
        {
            _iconAdded = true;
        }
        else
        {
            _logger.LogError("Shell_NotifyIcon(NIM_ADD) failed");
        }
    }

    // Removes the icon from the tray.
    private void RemoveIcon()
    {
        if (!_iconAdded)
        {
            return;
        }

        var data = CreateIconData(0);
        Shell_NotifyIcon(NIM_DELETE, ref data);
        _iconAdded = false;
    }

    // Fills in the NOTIFYICONDATA that identifies our icon for a Shell_NotifyIcon call. uFlags says which of the optional fields are
    // meaningful for this particular call.
    private NOTIFYICONDATA CreateIconData(uint flags)
    {
        return new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = TrayIconId,
            uFlags = flags,
            uCallbackMessage = WM_TRAYCALLBACK,
            hIcon = _hIcon,
            szTip = _tooltip,
            szInfo = string.Empty,
            szInfoTitle = string.Empty,
        };
    }

    // Runs a menu action, swallowing (and logging) any exception so a faulty handler can't crash the tray thread and kill the message loop.
    private void SafeInvoke(Action? action)
    {
        try
        {
            action?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError("Tray menu action failed: {ex}", ex);
        }
    }

    // ------------------------------------------------------------------------------------------------------------------------------------
    // Everything below is the raw Win32 surface. The WndProc delegate type, the structs passed by-ref to the APIs, and the DllImport
    // declarations, etc... These mirror the definitions in the Windows headers.
    // ------------------------------------------------------------------------------------------------------------------------------------

    // The managed signature of a window procedure. Marshal.GetFunctionPointerForDelegate turns _wndProc into the raw function pointer
    // stored in WNDCLASSEX.lpfnWndProc.
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // The tray icon's description, passed to Shell_NotifyIcon. The ByValTStr strings are fixed-size inline buffers in the native struct
    // (which is why DllImport is used; LibraryImport can't handle this).
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;

        public uint dwState;
        public uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;

        public uint uVersionOrTimeout;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;

        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    // The window-class template registered with RegisterClassEx before the window is created.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public int cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? lpszMenuName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;

        public IntPtr hIconSm;
    }

    // A single queued window message, filled in by GetMessage.
    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    // A screen coordinate (used for the cursor position the menu pops up at).
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    // Registers the window class (the template CreateWindowEx builds our window from).
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    // Creates the hidden message-only window.
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle,
        string lpClassName,
        string lpWindowName,
        uint dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam
    );

    // Default processing for any message WndProc chooses not to handle.
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // Destroys the window (raises WM_DESTROY).
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    // Drops the window-class registration at shutdown.
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

    // The blocking message-pump primitives: retrieve, translate, and dispatch queued messages, plus post the quit message that makes
    // GetMessage return 0 and end the loop.
    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    // Posts a message to a window's queue without waiting. How other threads hand work to the tray thread.
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // Loads the .ico from disk into an icon handle, and frees it again at shutdown.
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    // Menu construction and display. Creates an empty menu, appends items/submenus, shows it, reads back the choice, and then frees it.
    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, int uIDNewItem, string? lpNewItem);

    // A MF_POPUP variant. uIDNewItem carries the submenu handle (a pointer), so it must be marshalled as IntPtr rather than the int command
    // id used by the overload above.
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "AppendMenuW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenuSub(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hWnd, IntPtr lptpm);

    // Get where the mouse currently is.
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    // Looks up a system metric (We use this for the recommended small-icon dimensions).
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    // Adds, removes, or updates the tray icon.
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    // The module handle used as the hInstance for the window class.
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
