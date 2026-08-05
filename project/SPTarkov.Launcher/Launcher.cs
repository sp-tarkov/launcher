using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using Photino.Blazor;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.Extensions;
using SPTarkov.Core.Helpers;
using SPTarkov.Core.Patching;
using SPTarkov.Launcher.Helpers;
using SPTarkov.Launcher.Platform;

namespace SPTarkov.Launcher;

public class Launcher
{
    public static PhotinoBlazorApp App { get; set; } = null!;
    private static ManifestEmbeddedFileProvider EmbedProvider { get; set; } = null!;
    private static ConfigHelper ConfigHelper { get; set; } = null!;

    private static int _visibleStateDuration = 2000;
    private static int _showTransitionDuration = 100;
    private static int _hideTransitionDuration = 100;
    private static string _appTitle = TitleHelper.AppName;
    private static ILogger<Launcher> _logger = null!;
    private static TrayHelper _trayHelper = null!;
    private static bool _exitRequested;
    private static string? _trayIconPath;
    private static SingleInstanceGuard _singleInstanceGuard = null!;
    private static readonly string TemporaryFilesPath = Path.Join(Path.GetTempPath(), "SPT.Launcher");

    // Photino re-asserts the window while cancelling a close so wait before hiding to tray.
    private const int HideToTrayDelayMs = 100;

    [STAThread]
    private static void Main(string[] args)
    {
        SetNvidiaLinuxEnv();

        // Single-instance guard scoped to install location.
        _singleInstanceGuard = new SingleInstanceGuard();
        if (!_singleInstanceGuard.TryClaimPrimary())
        {
            _singleInstanceGuard.Dispose();
            return;
        }

        EmbedProvider = new ManifestEmbeddedFileProvider(typeof(Launcher).Assembly, "wwwroot");
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(EmbedProvider, args);

        appBuilder
            .Services.AddSingleton<ConfigHelper>()
            .AddSingleton<GameHelper>()
            .AddSingleton<HttpHelper>()
            .AddSingleton<StateHelper>()
            .AddSingleton<TitleHelper>()
            .AddSingleton<TrayHelper>()
            .AddSingleton<SessionHelper>()
            .AddSingleton<LocaleHelper>()
            .AddSingleton<FilePatcher>()
            .AddSingleton<WindowsClipboard>()
            .AddSingleton<LinuxHelper>()
            .AddSingleton<BrowserBridge>()
            .AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
                builder.AddFileLogger();
            })
            .AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
                config.SnackbarConfiguration.PreventDuplicates = false;
                config.SnackbarConfiguration.MaxDisplayedSnackbars = 3;
                config.SnackbarConfiguration.NewestOnTop = true;
                config.SnackbarConfiguration.VisibleStateDuration = _visibleStateDuration;
                config.SnackbarConfiguration.ShowTransitionDuration = _showTransitionDuration;
                config.SnackbarConfiguration.HideTransitionDuration = _hideTransitionDuration;
            });

        appBuilder.RootComponents.Add<App>("app");

        App = appBuilder.Build();

        _logger = App.Services.GetRequiredService<ILogger<Launcher>>();
        ConfigHelper = App.Services.GetRequiredService<ConfigHelper>();
        _trayHelper = App.Services.GetRequiredService<TrayHelper>();

        CustomizeComponent();

        // Listen for a second launch asking us to surface it.
        _singleInstanceGuard.StartActivationListener(SurfaceMainWindow, _logger);

        AppDomain.CurrentDomain.UnhandledException += (_, error) =>
        {
            App.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
        };

        try
        {
            App.Run();
        }
        catch (Exception e)
        {
            ValidateRuntimeEnvironment(e);
            throw;
        }
        finally
        {
            // Runs on the main thread once the message loop exits.
            _trayHelper.Dispose();
            _singleInstanceGuard.Dispose();
            DeleteTrayIcon();
        }
    }

    private static void SetNvidiaLinuxEnv()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        // Linux and Nvidia have issues with webkit, to fix those issues set an env variable
        // https://github.com/NVIDIA/egl-wayland/blob/master/src/wayland-egldisplay.c#L1241
        // https://bugs.webkit.org/show_bug.cgi?id=280210
        // this should only affect nvidia users, so no need to condition on AMD/INTEL
        // Environment.SetEnvironmentVariable("__NV_DISABLE_EXPLICIT_SYNC", "1");
        // this doesn't work completely on Unix, FML. going deeper.

        if (LinuxHelper.SetEnvironmentVariableNative("__NV_DISABLE_EXPLICIT_SYNC", "1", 1) != 0)
        {
            throw new InvalidOperationException(
                $"Failed to set __NV_DISABLE_EXPLICIT_SYNC: error number: {Marshal.GetLastPInvokeErrorMessage()}"
            );
        }
    }

    private static void CustomizeComponent()
    {
        Directory.CreateDirectory(TemporaryFilesPath);
        App.MainWindow.SetTemporaryFilesPath(TemporaryFilesPath).SetTitle(_appTitle);

        // Use extension method to get icon from embedded resource
        App.MainWindow.SetIconFile(
            EmbedProvider
                .GetDirectoryContents("images")
                .FirstOrDefault(x => x.Name.ToLower().Contains("spt-logo.ico"))
                ?.CreateReadStream()!,
            "spt-logo.ico"
        );

        // Wire "Close To Tray". Launcher owns the window ops, TrayHelper owns the native tray icon.
        _trayHelper.Configure(ExtractTrayIcon(), onRestore: SurfaceMainWindow, onExit: ExitFromTray);

        // Shut photino up
        App.MainWindow.LogVerbosity = 0;

#if !DEBUG
        // use this to disable bottom left status bar like in a browser
        App.MainWindow.DevToolsEnabled = false;

        // This is not needed and will break on linux
        if (OperatingSystem.IsWindows())
        {
            App.MainWindow.BrowserControlInitParameters = "--kiosk";
        }
        App.MainWindow.ContextMenuEnabled = false;
#else
        App.MainWindow.DevToolsEnabled = true;
        App.MainWindow.ContextMenuEnabled = true;
#endif
        App.MainWindow.Topmost = ConfigHelper.GetConfig().AlwaysTop;
        App.MainWindow.MinHeight = 560;
        App.MainWindow.MinWidth = 1070;

        // Changing monitors ends up breaking position restore for some users, so we are going to skip that
        App.MainWindow.SetUseOsDefaultLocation(false);
        App.MainWindow.Center();

        if (ConfigHelper.GetConfig().FirstRun)
        {
            App.MainWindow.Width = 1070;
            App.MainWindow.Height = 560;
        }
        else
        {
            App.MainWindow.Width = ConfigHelper.GetConfig().StartSize.Width;
            App.MainWindow.Height = ConfigHelper.GetConfig().StartSize.Height;
        }

        App.MainWindow.RegisterWindowClosingHandler(OnExit);
        App.MainWindow.SetMinimized(true);
    }

    private static bool OnExit(object sender, EventArgs e)
    {
        // When exiting from the tray the window is already hidden, so its size was saved at hide time.
        if (!_exitRequested)
        {
            ConfigHelper.SetClientLocation(App.MainWindow.Top, App.MainWindow.Left);
            ConfigHelper.SetClientSize(App.MainWindow.Height, App.MainWindow.Width);
            ConfigHelper.SetFirstRun(false);
        }

        // Close To Tray. Hide the window instead of exiting.
        if (!_exitRequested && OperatingSystem.IsWindows() && ConfigHelper.GetConfig().CloseToTray)
        {
            HideToTray();
            return true;
        }

        return false;
    }

    private static void HideToTray()
    {
        _trayHelper.Show();

        // Photino re-shows the window when it cancels the close, so hiding here is immediately undone. Defer the hide onto the UI thread so
        // it runs after the close has been fully cancelled.
        _ = Task.Run(async () =>
        {
            await Task.Delay(HideToTrayDelayMs);
            App.MainWindow.Invoke(() => WindowNative.ShowWindow(App.MainWindow.WindowHandle, WindowNative.SW_HIDE));
        });
    }

    // Brings the window back into focus.
    private static void SurfaceMainWindow()
    {
        _trayHelper.Hide();

        // May run on the tray or single-instance thread.
        App.MainWindow.Invoke(() =>
        {
            var handle = App.MainWindow.WindowHandle;
            WindowNative.ShowWindow(handle, WindowNative.SW_RESTORE);
            WindowNative.SetForegroundWindow(handle);
        });
    }

    private static void ExitFromTray()
    {
        _exitRequested = true;
        App.MainWindow.Close();
    }

    private static string ExtractTrayIcon()
    {
        // Different installations run concurrently (single-instance is per-install) and share the one user temp dir, so a fixed filename
        // would let their startup writes race. Keying the name on the process id gives each instance its own file.
        var tempPath = Path.Join(TemporaryFilesPath, $"spt-logo-{Environment.ProcessId}.ico");

        try
        {
            var iconStream = EmbedProvider
                .GetDirectoryContents("images")
                .FirstOrDefault(x => x.Name.ToLower().Contains("spt-logo.ico"))
                ?.CreateReadStream();

            if (iconStream is not null)
            {
                using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                iconStream.CopyTo(fileStream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to extract tray icon: {ex}", ex);
        }

        _trayIconPath = tempPath;
        return tempPath;
    }

    // Removes this instance's tray icon temp file on shutdown. Best-effort.
    private static void DeleteTrayIcon()
    {
        if (_trayIconPath is null)
        {
            return;
        }

        try
        {
            if (File.Exists(_trayIconPath))
            {
                File.Delete(_trayIconPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to remove tray icon temp file: {ex}", ex);
        }
    }

    private static void ValidateRuntimeEnvironment(Exception e)
    {
        _logger.LogCritical("Exception occured: {Exception}", e);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogCritical("Please check the following is installed:");
            _logger.LogCritical("WebView2 - https://developer.microsoft.com/en-us/microsoft-edge/webview2");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            _logger.LogCritical("Please check the following is installed:");
            _logger.LogCritical("webkit2gtk-4.1");
        }
    }
}
