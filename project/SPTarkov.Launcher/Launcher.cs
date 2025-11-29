using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using Photino.Blazor;
using SevenZip;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.Extensions;
using SPTarkov.Core.Helpers;
using SPTarkov.Core.Mods;
using SPTarkov.Core.Patching;
using SPTarkov.Core.SPT;

namespace SPTarkov.Launcher;

public class Launcher
{
    public static PhotinoBlazorApp App { get; set; }
    private static ManifestEmbeddedFileProvider EmbedProvider { get; set; }
    private static ConfigHelper ConfigHelper { get; set; }

    private static int _visibleStateDuration = 2000;
    private static int _showTransitionDuration = 100;
    private static int _hideTransitionDuration = 100;
    private static string _openExternalString = "open-external:";
    private static string _appTitle = "SPTarkov Launcher";
    private static ILogger<Launcher> _logger;

    [STAThread]
    private static void Main(string[] args)
    {
        EmbedProvider = new ManifestEmbeddedFileProvider(typeof(Launcher).Assembly, "wwwroot");
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(EmbedProvider, args);
        SevenZipBase.SetLibraryPath(Paths.SevenZipDllPath);

        appBuilder.Services
            .AddSingleton<ConfigHelper>()
            // .AddSingleton<ForgeHelper>()
            .AddSingleton<GameHelper>()
            .AddSingleton<HttpHelper>()
            .AddSingleton<ModManager>()
            .AddSingleton<DownloadHelper>()
            .AddSingleton<StateHelper>()
            .AddSingleton<LocaleHelper>()
            .AddSingleton<FilePatcher>()
            .AddSingleton<WindowsClipboard>()
            .AddSingleton<WineHelper>()
            .AddSingleton<ValidationUtil>()
            .AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
                builder.AddFileLogger();
            })
            .AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
                config.SnackbarConfiguration.PreventDuplicates = false;
                config.SnackbarConfiguration.VisibleStateDuration = _visibleStateDuration;
                config.SnackbarConfiguration.ShowTransitionDuration = _showTransitionDuration;
                config.SnackbarConfiguration.HideTransitionDuration = _hideTransitionDuration;
            });

        appBuilder.RootComponents.Add<App>("app");

        App = appBuilder.Build();

        _logger = App.Services.GetService<ILogger<Launcher>>();
        ConfigHelper = App.Services.GetService<ConfigHelper>();

        CustomizeComponent();

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
    }

    private static void CustomizeComponent()
    {
        App.MainWindow.SetTitle(_appTitle);

        // Use extension method to get icon from embedded resource
        App.MainWindow.SetIconFile(
            EmbedProvider.GetDirectoryContents("/")
                .FirstOrDefault(x => x.Name.ToLower().Contains("spt-logo.ico"))?
                .CreateReadStream()!,
            "spt-logo.ico"
        );

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
#endif
        App.MainWindow.Topmost = ConfigHelper.GetConfig().AlwaysTop;
        App.MainWindow.MinHeight = 550;
        App.MainWindow.MinWidth = 1070;

        if (ConfigHelper.GetConfig().FirstRun)
        {
            App.MainWindow.Width = 1070;
            App.MainWindow.Height = 550;
            App.MainWindow.SetUseOsDefaultLocation(true);
        }
        else
        {
            App.MainWindow.Width = ConfigHelper.GetConfig().StartSize.Width;
            App.MainWindow.Height = ConfigHelper.GetConfig().StartSize.Height;

            App.MainWindow.SetUseOsDefaultLocation(false);

            App.MainWindow.Top = ConfigHelper.GetConfig().StartLocation.X;
            App.MainWindow.Left = ConfigHelper.GetConfig().StartLocation.Y;
        }

        App.MainWindow.RegisterWindowClosingHandler(OnExit);
        App.MainWindow.SetMinimized(true);

        App.MainWindow.RegisterWebMessageReceivedHandler((_, message) =>
        {
            if (message.StartsWith(_openExternalString))
            {
                var url = message.Substring(_openExternalString.Length);
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to open URL: {ex}", ex);
                }
            }
        });
    }

    private static bool OnExit(object sender, EventArgs e)
    {
        ConfigHelper.SetClientLocation(App.MainWindow.Top, App.MainWindow.Left);
        ConfigHelper.SetClientSize(App.MainWindow.Height, App.MainWindow.Width);
        ConfigHelper.SetFirstRun(false);

        // See notes in BasicPanelComponent
        // if (ConfigHelper.GetConfig().CloseToTray)
        // {
        //     App.MainWindow.SetMinimized(true);
        //     return true;
        // }

        return false;
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
            _logger.LogCritical("Libwebkit2gtk-4.1");
        }
    }
}
