using System.Diagnostics;
using System.Text;
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

        // this looks something like this: "GE-Proton10-24"
        var proton = _configHelper.GetConfig().LinuxSettings.ProtonVersion;

        if (string.IsNullOrEmpty(prefixPath) || string.IsNullOrEmpty(umuPath) || string.IsNullOrEmpty(proton))
        {
            _logger.LogError("Prefix path or umu path or proton version are required");
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
                { "PROTONPATH", proton },
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

        var launchSettings = ParseLaunchSettings();
        foreach (var launchSetting in launchSettings)
        {
            if (launchSetting.Key.StartsWith("-"))
            {
                // this should be an arg
                process.ArgumentList.Add($"{launchSetting.Key}={launchSetting.Value}");
            }
            else
            {
                // this should be an env
                process.Environment.Add(launchSetting.Key, launchSetting.Value);
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

    // TODO: Maybe some Regex Guru can make this simplier
    public Dictionary<string, string> ParseLaunchSettings()
    {
        var launchSettings = _configHelper.GetConfig().LinuxSettings.LaunchSettings;
        var result = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(launchSettings))
        {
            return result;
        }

        // So parsing this string into usable EnvVar's and Arguments might be a bit weird
        // Most of the time i'd expect no spaces:
        // EnvironmentVariableName=EnvironmentVariableValue
        // -ArgumentName=ArgumentValue
        // The - is the diff between them and EnvVar's afaik are all uppercase
        // The only Edge Case I can think of is using a path as a variable in either. which would mean " " wrap the variable
        // -ArgWantingPath="/path/to/something with a space"
        // And because of this means I cant just split on whitespace.

        // Trim outer edge
        // MANGOHUD=1 -arg1=testing -arg2="some path with spaces/in it"
        launchSettings = launchSettings.Trim();

        var stringBuilder = new StringBuilder();
        var name = string.Empty;
        var value = string.Empty;
        var isName = true; // Start as true as this comes first
        var isValue = false;
        var valueHasQuotes = false;
        var checkedForQuotes = false;
        var reset = false;

        try
        {
            foreach (var charFromStr in launchSettings)
            {
                // Go through the string till we hit a =
                // we now want to deal with the value
                if (isName && charFromStr == '=')
                {
                    isName = false;
                    isValue = true;
                    name = stringBuilder.ToString();
                    stringBuilder.Clear();
                    continue;
                }

                if (isValue && checkedForQuotes && !valueHasQuotes && charFromStr == ' ')
                {
                    // end of Value, wasnt quotes so is whitespace
                    // dont append and reset trackers and continue;
                    value = stringBuilder.ToString();
                    reset = true;
                }

                if (isValue && valueHasQuotes && charFromStr == '"')
                {
                    // this should be the end of the quoted string
                    stringBuilder.Append("\"");
                    value =  stringBuilder.ToString();
                    reset = true;
                }

                // the value could start with quotes " - this means we have to handle this variable slightly differently
                if (isValue && !checkedForQuotes)
                {
                    // check to see if the first char is a "
                    valueHasQuotes = charFromStr == '"';
                    checkedForQuotes = true;
                }

                if (isValue && reset)
                {
                    result.Add((string) name.Clone(), (string) value.Clone());
                    name = string.Empty;
                    value = string.Empty;
                    isName = true;
                    isValue = false;
                    valueHasQuotes = false;
                    checkedForQuotes = false;
                    stringBuilder.Clear();
                    reset = false;
                    continue;
                }

                // At the end of the value, there should be a space between envs and args
                if (isName && charFromStr == ' ')
                {
                    // We should be able to skip this
                    continue;
                }

                stringBuilder.Append(charFromStr);
            }

            // check if the name and value have anything at the end, if so, last arg/env had no space or " so add whats there
            if (!string.IsNullOrEmpty(name))
            {
                value = stringBuilder.ToString();
                result.Add((string) name.Clone(), (string) value.Clone());
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning("unable to parse launch Settings of: {setting}, please format correctly: {e}",  launchSettings, e);
            return new Dictionary<string, string>();
        }

        return result;
    }

    private readonly string _protonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".local/share/Steam/compatibilitytools.d");

    public Task<List<string>> GetProtonVersions()
    {
        // Should contain things like "GE-Proton10-24" or "GE-Proton10-21"
        // Could be named slightly different if user downloads "custom" ones like "EM-10.0-30"
        var directoryContents = Directory.GetDirectories(_protonPath);
        var listStripped = new List<string>();

        foreach (var directory in directoryContents)
        {
            // remove LegacyRuntime
            if (directory.Contains("LegacyRuntime"))
            {
                continue;
            }

            // split on / and get last
            listStripped.Add(directory.Split("/").Last());
        }

        return Task.FromResult(listStripped);
    }
}
