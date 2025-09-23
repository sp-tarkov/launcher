using System.Text.Json;
using Microsoft.Extensions.Logging;
using MudBlazor;
using SPTarkov.Core.Logging;
using SPTarkov.Core.Models;

namespace SPTarkov.Core.Helpers;

public class ConfigHelper
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly Lock _lock = new();
    private readonly ILogger<ConfigHelper> _logger;

    public readonly DialogOptions DialogOptions = new()
    {
        Position = DialogPosition.Center,
        MaxWidth = MaxWidth.ExtraSmall,
        BackdropClick = true,
        CloseOnEscapeKey = true,
        NoHeader = true,
        FullWidth = true,
        BackgroundClass = "dialog-backdrop-class"
    };

    private readonly string LauncherAssetsPath = Path.Combine(Environment.CurrentDirectory, "user", "Launcher");

    private LauncherSettings? _settings;

    public ConfigHelper(
        ILogger<ConfigHelper> logger
    )
    {
        _logger = logger;
        LoadSettingsFromFile();
    }

    private void LoadSettingsFromFile()
    {
        lock (_lock)
        {
            _logger.LogInformation("LoadSettingsFromFile.");

            if (!File.Exists(Path.Combine(LauncherAssetsPath, "LauncherSettings.json")))
            {
                SaveDefaults();
            }

            _settings = JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(Path.Combine(LauncherAssetsPath, "LauncherSettings.json")));

            // Set the base game path to the launcher directory, there's no reason to have this outside of the game directory
            SetGamePath(Environment.CurrentDirectory);
        }
    }

    public LauncherSettings GetConfig()
    {
        lock (_lock)
        {
            return _settings!;
        }
    }

    private void SaveConfig()
    {
        lock (_lock)
        {
            File.WriteAllText(Path.Combine(LauncherAssetsPath, "LauncherSettings.json"), JsonSerializer.Serialize(_settings, _jsonOptions));
        }
    }

    public void SetClientSize(int height, int width)
    {
        lock (_lock)
        {
            _logger.LogInformation("SetClientSize: {Height}-{Width}", height, width);
            _settings!.StartSize.Height = height;
            _settings!.StartSize.Width = width;
            SaveConfig();
        }
    }

    public void SetClientLocation(int x, int y)
    {
        lock (_lock)
        {
            _logger.LogInformation("SetClientLocation: {x}-{y}", x, y);
            _settings!.StartLocation.X = x;
            _settings!.StartLocation.Y = y;
            SaveConfig();
        }
    }

    public void SetFirstRun(bool firstRun)
    {
        lock (_lock)
        {
            _logger.LogInformation("SetFirstRun: {FirstRun}", firstRun);
            _settings!.FirstRun = firstRun;
            SaveConfig();
        }
    }

    public void SetServers(List<Server> servers)
    {
        lock (_lock)
        {
            _logger.LogInformation("SetServers: {ServersCount}", servers.Count);
            _settings!.Servers = servers;
            SaveConfig();
        }
    }

    public void SetCloseToTray(bool closeToTray)
    {
        lock (_lock)
        {
            _logger.LogInformation("SetCloseToTray: {CloseToTray}", closeToTray);
            _settings!.CloseToTray = closeToTray;
            SaveConfig();
        }
    }

    public void SetMinimizeOnLaunch(bool minimizeOnLaunch)
    {
        lock (_lock)
        {
            _logger.LogInformation("SetMinimizeOnLaunch: {MinimizeOnLaunch}", minimizeOnLaunch);
            _settings!.MinimizeOnLaunch = minimizeOnLaunch;
            SaveConfig();
        }
    }

    public void SetAlwaysOnTop(bool alwaysOnTop)
    {
        lock (_lock)
        {
            _logger.LogInformation("SetAlwaysOnTop: {AlwaysOnTop}", alwaysOnTop);
            _settings!.AlwaysTop = alwaysOnTop;
            SaveConfig();
        }
    }

    public void SetApiKey(string apiKey)
    {
        lock (_lock)
        {
            _logger.LogInformation("SetApiKey: {ApiKey}", apiKey);
            _settings!.ForgeApiKey = apiKey;
            SaveConfig();
        }
    }

    public void SetUseBackground(bool useBackground)
    {
        lock (_lock)
        {
            _logger.LogInformation("SetUseBackground: {UseBackground}", useBackground);
            _settings!.UseBackground = useBackground;
            SaveConfig();
        }
    }

    private void SetGamePath(string gamePath)
    {
        lock (_lock)
        {
            _logger.LogInformation("SetGamePath: {GamePath}", gamePath);
            _settings!.GamePath = gamePath;
            SaveConfig();
        }
    }

    private void SaveDefaults()
    {
        lock (_lock)
        {
            _logger.LogInformation("SaveDefaults.");
            Directory.CreateDirectory(LauncherAssetsPath);
            File.WriteAllText(Path.Combine(LauncherAssetsPath, "LauncherSettings.json"), JsonSerializer.Serialize(new LauncherSettings(), _jsonOptions));
        }
    }

    public void SetLocale(string locale)
    {
        lock (_lock)
        {
            _logger.LogInformation("SetLocale: {Locale}", locale);
            _settings!.Language = locale;
            SaveConfig();
        }
    }
}
