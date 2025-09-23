using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using Photino.Blazor;
using SPTarkov.Core.Extensions;
using SPTarkov.Core.Helpers;
using SPTarkov.Core.Logging;
using SPTarkov.Core.Patching;

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
    private static ILogger<Launcher> _logger;

    [STAThread]
    private static void Main(string[] args)
    {
        EmbedProvider = new ManifestEmbeddedFileProvider(typeof(Launcher).Assembly, "wwwroot");
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(EmbedProvider, args);

        appBuilder.Services
            .AddSingleton<ConfigHelper>()
            .AddSingleton<ForgeHelper>()
            .AddSingleton<GameHelper>()
            .AddSingleton<HttpHelper>()
            .AddSingleton<ModHelper>()
            .AddSingleton<StateHelper>()
            .AddSingleton<LocaleHelper>()
            .AddSingleton<FilePatcher>()
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
        var http = App.Services.GetService<HttpHelper>();
        var modLoader = App.Services.GetService<ModHelper>();

        http.IsInternetAccessAvailable();
        _ = modLoader.GetClientMods();
        _ = modLoader.GetServerMods();

        CustomizeComponent();

        AppDomain.CurrentDomain.UnhandledException += (_, error) => { App.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString()); };

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
        App.MainWindow.SetTitle("SPTarkov Launcher");
        App.MainWindow.DevToolsEnabled = true;
        App.MainWindow.LogVerbosity = 0;

        // use this to disable bottom left status bar like in a browser
        // TODO: comment out to gain devtools - this flag disables it.
        // App.MainWindow.BrowserControlInitParameters = "--kiosk";
        // App.MainWindow.ContextMenuEnabled = false;

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

        if (ConfigHelper.GetConfig().CloseToTray)
        {
            App.MainWindow.SetMinimized(true);
            return true;
        }

        return false;
    }

    private static void ValidateRuntimeEnvironment(Exception e)
    {
        _logger.LogCritical("Exception occured: {Exception}", e);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // TODO: check if the exception is related to missing webview2 deps.
            _logger.LogCritical("Please check the following is installed:");
            _logger.LogCritical("WebView2 - https://developer.microsoft.com/en-us/microsoft-edge/webview2");
            _logger.LogCritical("DotNet 9.0 runtime - https://dotnet.microsoft.com/en-us/download/dotnet/9.0");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // TODO: check if the exception is related to missing webkit2gtk4.1 deps.
            _logger.LogCritical("Please check the following is installed:");
            _logger.LogCritical("Libwebkit2gtk-4.1");
            _logger.LogCritical("DotNet 9.0 runtime");
        }
    }
}
