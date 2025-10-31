using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Configuration;

namespace SPTarkov.Core.Helpers;

public class WineHelper
{
    private readonly ILogger<WineHelper> _logger;
    private readonly ConfigHelper _configHelper;

    public WineHelper(
        ILogger<WineHelper> logger,
        ConfigHelper configHelper
    )
    {
        _logger = logger;
        _configHelper = configHelper;
    }

    // Vibe-coded wine registry file parser :D from MadByte, To test and check
    public string? FindWineRegValue(string file, string key, string valueName)
    {
        var reader = new StreamReader(file);
        string? line;
        string? secondLine;
        var foundIt = false;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();

            if (line.StartsWith("["))
            {
                foundIt = line.Contains(key);
            }

            if (foundIt)
            {
                while ((secondLine = reader.ReadLine()) != null)
                {
                    if (secondLine.Contains(valueName))
                    {
                        return secondLine.Substring(19, secondLine.Length - 19 - 1);
                    }
                }
            }
        }

        return null;
    }

    public string? GetOriginalGamePath()
    {
        var prefixPath = _configHelper.GetConfig().LinuxSettings.PrefixPath;
        if (string.IsNullOrEmpty(prefixPath))
        {
            _logger.LogError("Prefix path is required");
            return null;
        }

        var regFilePath = Path.Combine(prefixPath, "system.reg");
        // must contain \\ for windows reg key when looking on linux/wine
        var key = @"Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\EscapeFromTarkov";

        try
        {
            var windowsLikePath = FindWineRegValue(regFilePath, key, "InstallLocation");
            return FixWithPrefix(windowsLikePath);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get EFT game path: {ex}");
            return null;
        }
    }

    private string FixWithPrefix(string? windowsLikePath)
    {
        var pathWithoutDrive = windowsLikePath?.Replace("\\\\", "/").Substring(2);
        _logger.LogDebug("pathWithoutDrive: {0}", pathWithoutDrive);
        return string.Concat(_configHelper.GetConfig().LinuxSettings.PrefixPath, pathWithoutDrive);
    }

    // Example commands (to explain what would be possible using umu-run):
    // _wineHelper.RunInPrefix("EscapeFromTarkov.exe", args) -- Launch any executable in the current working dir
    // _wineHelper.RunInPrefix("winecfg", args) -- Launch the winecfg menu
    // _wineHelper.RunInPrefix("winetricks", "-q win11") -- To set the windows version of the prefix to Windows 11
    // _wineHelper.RunInPrefix("winetricks", "-q dotnetdesktop9") -- To install .NET Desktop 9 automatically
    // _wineHelper.RunInPrefix("regedit") -- To launch the regedit tool (Pretty much the same as on windows)
    public async Task<bool> RunInPrefix(string cmd = "", List<string>? args = null)
    {
        // This looks something like: "/home/{username}/Games/tarkov"
        // However this could be anything the user sets it too when they use MadBytes script.
        var prefixPath = _configHelper.GetConfig().LinuxSettings.PrefixPath;

        // This looks something like this: "/home/{username}/.local/bin/umu-run"
        var umuPath = _configHelper.GetConfig().LinuxSettings.UmuPath;

        if (string.IsNullOrEmpty(prefixPath) || string.IsNullOrEmpty(umuPath))
        {
            _logger.LogError("Prefix path or umu path are required");
            return false;
        }

        // this looks something like: "/home/{username}/Games/tarkov/drive_c/SPTarkov"
        var sptPath = _configHelper.GetConfig().GamePath;

        var process = new ProcessStartInfo
        {
            FileName = "python3",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = sptPath,
            Environment =
            {
                { "WINEPREFIX", prefixPath },
                { "DOTNET_ROOT", ""},
                { "DOTNET_BUNDLE_EXTRACT_BASE_DIR", ""},
                { "PROTON_USE_XALIA", "0"},
                { "PROTONPATH", "GE-Proton10-24"},
            },
            ArgumentList =
            {
                umuPath,
                cmd
            }
        };

        // Add these individually so they are not wrapped in ""
        if (args != null)
        {
            foreach (var arg in args)
            {
                process.ArgumentList.Add(arg);
            }
        }

        try
        {
            Process.Start(process);
            _logger.LogInformation("Game process started on linux");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Starting game process failed: {ex}");
            return false;
        }

        return true;
    }
}
