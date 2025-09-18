using System.Text.Json;
using MudBlazor;
using SPTarkov.Core.Models;

namespace SPTarkov.Core.Helpers;

public class ConfigHelper
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly Lock _lock = new();
    private readonly LogHelper _logHelper;

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

    private readonly string LauncherAssetsPath = Path.Combine(Environment.CurrentDirectory, "SPT_Data", "Launcher");

    private LauncherSettings? _settings;

    public ConfigHelper(
        LogHelper logHelper
    )
    {
        _logHelper = logHelper;
        LoadSettingsFromFile();
    }

    private void LoadSettingsFromFile()
    {
        lock (_lock)
        {
            _logHelper.LogInfo("LoadSettingsFromFile.");

            if (!File.Exists(Path.Combine(LauncherAssetsPath, "LauncherSettings.json")))
            {
                SaveDefaults();
            }

            _settings = JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(Path.Combine(LauncherAssetsPath, "LauncherSettings.json")));
        }
    }

    public LauncherSettings GetConfig()
    {
        lock (_lock)
        {
            return _settings!;
        }
    }

    public void SaveConfig()
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
            _logHelper.LogInfo($"SetClientSize: {height}, {width}");
            _settings!.StartSize.Height = height;
            _settings!.StartSize.Width = width;
            SaveConfig();
        }
    }

    public void SetClientLocation(int x, int y)
    {
        lock (_lock)
        {
            _logHelper.LogInfo($"SetClientLocation: {x},{y}");
            _settings!.StartLocation.X = x;
            _settings!.StartLocation.Y = y;
            SaveConfig();
        }
    }

    public void SetFirstRun(bool firstRun)
    {
        lock (_lock)
        {
            _logHelper.LogInfo($"SetFirstRun: {firstRun}");
            _settings!.FirstRun = firstRun;
            SaveConfig();
        }
    }

    public void SetServers(List<Server> servers)
    {
        lock (_lock)
        {
            _logHelper.LogInfo($"SetServers: {servers.Count}");
            _settings!.Servers = servers;
            SaveConfig();
        }
    }

    public void SetCloseToTray(bool closeToTray)
    {
        lock (_lock)
        {
            _logHelper.LogInfo($"SetCloseToTray: {closeToTray}");
            _settings!.CloseToTray = closeToTray;
            SaveConfig();
        }
    }

    public void SetMinimizeOnLaunch(bool minimizeOnLaunch)
    {
        lock (_lock)
        {
            _logHelper.LogInfo($"SetMinimizeOnLaunch: {minimizeOnLaunch}");
            _settings!.MinimizeOnLaunch = minimizeOnLaunch;
            SaveConfig();
        }
    }

    public void SetAlwaysOnTop(bool alwaysOnTop)
    {
        lock (_lock)
        {
            _logHelper.LogInfo($"SetAlwaysOnTop: {alwaysOnTop}");
            _settings!.AlwaysTop = alwaysOnTop;
            SaveConfig();
        }
    }

    public void SetAdvancedUser(bool advancedUser)
    {
        lock (_lock)
        {
            _logHelper.LogInfo($"SetAdvancedUser: {advancedUser}");
            _settings!.AdvancedUser = advancedUser;
            SaveConfig();
        }
    }

    public void SetDebugUser(bool debugUser)
    {
        lock (_lock)
        {
            _logHelper.LogInfo($"SetDebugUser: {debugUser}");
            _settings!.DebugSettings.DebugUser = debugUser;
            SaveConfig();
        }
    }

    public void SetDebugLoggingPage(bool access)
    {
        lock (_lock)
        {
            _logHelper.LogInfo($"SetDebugLoggingPage: {access}");
            _settings!.DebugSettings.ShowLoggingPage = access;
            SaveConfig();
        }
    }

    public void SetApiKey(string apiKey)
    {
        lock (_lock)
        {
            _logHelper.LogInfo($"SetApiKey: {apiKey}");
            _settings!.ForgeApiKey = apiKey;
            SaveConfig();
        }
    }

    public void SetUseBackground(bool useBackground)
    {
        lock (_lock)
        {
            _logHelper.LogInfo($"SetUseBackground: {useBackground}");
            _settings!.UseBackground = useBackground;
            SaveConfig();
        }
    }

    private void SaveDefaults()
    {
        lock (_lock)
        {
            _logHelper.LogInfo("SaveDefaults.");
            Directory.CreateDirectory(LauncherAssetsPath);
            File.WriteAllText(Path.Combine(LauncherAssetsPath, "LauncherSettings.json"), JsonSerializer.Serialize(new LauncherSettings(), _jsonOptions));
        }
    }
}
