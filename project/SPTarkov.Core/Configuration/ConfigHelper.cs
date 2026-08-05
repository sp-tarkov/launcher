using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MudBlazor;
using SPTarkov.Core.Logging;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.Configuration;

public class ConfigHelper
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
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
        BackgroundClass = "dialog-backdrop-class",
    };

    private LauncherSettings? _settings;

    public ConfigHelper(ILogger<ConfigHelper> logger)
    {
        _logger = logger;
        LoadSettingsFromFile();
        FileLogger.LogLevel = _settings!.DebugLogging ? LogLevel.Debug : LogLevel.Information;
    }

    private void LoadSettingsFromFile()
    {
        lock (_lock)
        {
            _logger.LogDebug("LoadSettingsFromFile.");

            if (!File.Exists(Paths.LauncherSettingsPath))
            {
                SaveDefaults();
            }

            _settings = JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(Paths.LauncherSettingsPath));

            // Settings files written by earlier builds (pushed out to BE) persisted the built-in server.
            // GetServers rebuilds it from code, so drop the stale copy to avoid showing it twice.
            // TODO: This can be removed after release of 4.1.0. Just want it for BE clean-up.
            var dropped = _settings!.Servers.RemoveAll(server => server.ServerId == Server.LocalServerId);
            if (dropped > 0)
            {
                _logger.LogDebug("Dropped {Count} persisted built-in server entries.", dropped);
            }
        }
    }

    private void SaveDefaults()
    {
        lock (_lock)
        {
            _logger.LogDebug("SaveDefaults.");
            Directory.CreateDirectory(Paths.LauncherAssetsPath);
            File.WriteAllText(Paths.LauncherSettingsPath, JsonSerializer.Serialize(new LauncherSettings(), _jsonOptions));
        }
    }

    public LauncherSettings GetConfig()
    {
        lock (_lock)
        {
            return _settings!;
        }
    }

    public string GetGamePath()
    {
        lock (_lock)
        {
            if (_settings!.EnableGamePath)
            {
                return _settings.GamePath;
            }
            else
            {
                return Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;
            }
        }
    }

    private void SaveConfig()
    {
        lock (_lock)
        {
            File.WriteAllText(Paths.LauncherSettingsPath, JsonSerializer.Serialize(_settings, _jsonOptions));
        }
    }

    public void SetClientSize(int height, int width)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetClientSize: h{Height} w{Width}", height, width);
            _settings!.StartSize.Height = height;
            _settings!.StartSize.Width = width;
            SaveConfig();
        }
    }

    public void SetClientLocation(int x, int y)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetClientLocation: x{x} y{y}", x, y);
            _settings!.StartLocation.X = x;
            _settings!.StartLocation.Y = y;
            SaveConfig();
        }
    }

    public void SetFirstRun(bool firstRun)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetFirstRun: {FirstRun}", firstRun);
            _settings!.FirstRun = firstRun;
            SaveConfig();
        }
    }

    public List<Server> GetServers()
    {
        lock (_lock)
        {
            // The built-in local server first, then whatever the user has added.
            return [Server.Local, .. _settings!.Servers];
        }
    }

    public void SetServers(List<Server> servers)
    {
        lock (_lock)
        {
            var userServers = servers.Where(server => server.ServerId != Server.LocalServerId).ToList();

            _logger.LogDebug("SetServers: {ServersCount}", userServers.Count);
            _settings!.Servers = userServers;
            SaveConfig();
        }
    }

    public void SetCloseToTray(bool closeToTray)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetCloseToTray: {CloseToTray}", closeToTray);
            _settings!.CloseToTray = closeToTray;
            SaveConfig();
        }
    }

    public void SetMinimizeOnLaunch(bool minimizeOnLaunch)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetMinimizeOnLaunch: {MinimizeOnLaunch}", minimizeOnLaunch);
            _settings!.MinimizeOnLaunch = minimizeOnLaunch;
            SaveConfig();
        }
    }

    public void SetAlwaysOnTop(bool alwaysOnTop)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetAlwaysOnTop: {AlwaysOnTop}", alwaysOnTop);
            _settings!.AlwaysTop = alwaysOnTop;
            SaveConfig();
        }
    }

    public void SetUseBackground(bool useBackground)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetUseBackground: {UseBackground}", useBackground);
            _settings!.UseBackground = useBackground;
            SaveConfig();
        }
    }

    public void SetLocale(string locale)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetLocale: {Locale}", locale);
            _settings!.Language = locale;
            SaveConfig();
        }
    }

    public void SetLinuxPrefixPath(string linuxPrefixPath)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetLinuxPrefixPath: {LinuxPrefixPath}", linuxPrefixPath);
            _settings!.LinuxSettings.PrefixPath = linuxPrefixPath;
            SaveConfig();
        }
    }

    public void SetLinuxUmuPath(string linuxUmuPath)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetLinuxUmuPath: {UmuPath}", linuxUmuPath);
            _settings!.LinuxSettings.UmuPath = linuxUmuPath;
            SaveConfig();
        }
    }

    public void SetLinuxLaunchSettings(string linuxLaunchSettings)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetLinuxLaunchSettings: {LaunchSettings}", linuxLaunchSettings);
            _settings!.LinuxSettings.LaunchSettings = linuxLaunchSettings;
            SaveConfig();
        }
    }

    public void SetLinuxProtonVersion(string linuxProtonVersion)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetLinuxProtonVersion: {ProtonVersion}", linuxProtonVersion);
            _settings!.LinuxSettings.ProtonVersion = linuxProtonVersion;
            SaveConfig();
        }
    }

    public void SetLinuxGameMode(bool linuxGameMode)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetLinuxGameMode: {GameMode}", linuxGameMode);
            _settings!.LinuxSettings.GameMode = linuxGameMode;
            SaveConfig();
        }
    }

    public void SetDebugLogging(bool debugLogging)
    {
        lock (_lock)
        {
            _logger.LogInformation("SetDebugLogging: {DebugLogging}", debugLogging);
            FileLogger.LogLevel = debugLogging ? LogLevel.Debug : LogLevel.Information;
            _settings!.DebugLogging = debugLogging;
            SaveConfig();
        }
    }

    public void SetClearCacheOnLaunch(bool clearCacheOnLaunch)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetClearCacheOnLaunch: {ClearCacheOnLaunch}", clearCacheOnLaunch);
            _settings!.ClearCacheOnLaunch = clearCacheOnLaunch;
            SaveConfig();
        }
    }

    public void SetEnableGamePath(bool enableGamePath)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetEnableGamePath: {EnableGamePath}", enableGamePath);
            _settings!.EnableGamePath = enableGamePath;
            SaveConfig();
        }
    }

    public void SetGamePath(string gamePath)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetGamePath: {GamePath}", gamePath);
            _settings!.GamePath = gamePath;
            SaveConfig();
        }
    }

    public void SetAutoConnectLastProfile(bool autoConnectLastProfile)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetAutoConnectLastProfile: {AutoConnectLastProfile}", autoConnectLastProfile);
            _settings!.AutoConnectLastProfile = autoConnectLastProfile;
            SaveConfig();
        }
    }

    public void SetPreferredProfile(string serverId, string profileId)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetPreferredProfile: server {ServerId} profile {ProfileId}", serverId, profileId);
            _settings!.PreferredProfile = new PreferredProfile { ServerId = serverId, ProfileId = profileId };
            SaveConfig();
        }
    }

    public void ClearPreferredProfile()
    {
        lock (_lock)
        {
            _logger.LogDebug("ClearPreferredProfile");
            _settings!.PreferredProfile = null;
            SaveConfig();
        }
    }

    public void SetLinuxSettings(LinuxSettings linuxSettings)
    {
        lock (_lock)
        {
            _logger.LogDebug("SetLinuxSettings: {Settings}", linuxSettings);
            _settings!.LinuxSettings = linuxSettings;
            SaveConfig();
        }
    }
}
