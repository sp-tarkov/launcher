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
    public static string? FindWineRegValue(string file, string key, string valueName)
    {
        using var reader = new StreamReader(file);
        string? line;
        var inSection = false;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                inSection = string.Equals(line.Substring(1, line.Length - 2), key, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (inSection)
            {
                if (line.StartsWith("["))
                    break; // new section, stop looking

                if (line.StartsWith($"\"{valueName}\""))
                {
                    int eq = line.IndexOf('=');
                    if (eq >= 0)
                        return line.Substring(eq + 1).Trim();
                }
                if (valueName == "@" && line.StartsWith("@="))
                {
                    return line.Substring(2).Trim();
                }
            }
        }

        return null;
    }

    public string? GetOriginalGamePath()
    {
        var prefixPath = _configHelper.GetConfig().LinuxSettings.PrefixPath;
        var regFilePath = Path.Combine(prefixPath, "system.reg");
        var key = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\EscapeFromTarkov";
        //var v1 = Registry.LocalMachine.OpenSubKey(c0, false)?.GetValue("InstallLocation"); maybe this works under the prefix

        try
        {
            return FindWineRegValue(regFilePath, key, "InstallLocation");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to get EFT game path: {ex}");
            return null;
        }
    }

    // Example commands (to explain what would be possible using umu-run):
    // _wineHelper.RunInPrefix("EscapeFromTarkov.exe", args) -- Launch any executable in the current working dir
    // _wineHelper.RunInPrefix("winecfg", args) -- Launch the winecfg menu
    // _wineHelper.RunInPrefix("winetricks", "-q win11") -- To set the windows version of the prefix to Windows 11
    // _wineHelper.RunInPrefix("winetricks", "-q dotnetdesktop9") -- To install .NET Desktop 9 automatically
    // _wineHelper.RunInPrefix("regedit") -- To launch the regedit tool (Pretty much the same as on windows)
    public async Task<bool> RunInPrefix(string cmd = "", string args = "")
    {
        // Linux SPT install will have a prefix where everything is installed.
        // This looks something like: "/home/{username}/Games/tarkov"
        // However this could be anything the user sets it too when they use MadBytes script.
        var prefixPath = _configHelper.GetConfig().LinuxSettings.PrefixPath; // these could be null

        // We'll assume Umu is already installed for now as its required for MadBytes script.
        // This looks something like this: "/home/{username}/.local/bin/umu-run"
        var umuPath = _configHelper.GetConfig().LinuxSettings.UmuPath; // these could be null

        // We'll assume its prefix + the usual for now.
        // this looks something like: "/home/{username}/Games/tarkov/drive_c/SPTarkov"
        var sptPath = Path.Combine(prefixPath, "drive_c", "SPTarkov");

        Environment.SetEnvironmentVariable("WINEPREFIX", prefixPath);
        Directory.SetCurrentDirectory(sptPath);

        // We can run umu-run either by doing `python $SCRIPT_PATH` or `bash -c '$SCRIPT_PATH'`
        var process = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            UseShellExecute = false,
            CreateNoWindow = true,
            ArgumentList =
            {
                "-c",
                umuPath,
                cmd,
                args
            }
        };

        try
        {
            Process.Start(process);
            _logger.LogInformation("Game process started");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Starting game process failed: {ex}");
            return false;
        }

        return true;
    }
}
