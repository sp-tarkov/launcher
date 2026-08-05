using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.Helpers;

public class LinuxHelper(ILogger<LinuxHelper> logger, ConfigHelper configHelper)
{
    /// <summary>
    /// reconstruct path used when installing EFT on linux to work on linux, using symlinks in dosdevice in the winePrefix
    /// </summary>
    /// <param name="windowsLikePath"></param>
    /// <returns></returns>
    public string FixWithPrefixValidation(string? windowsLikePath)
    {
        var pathAndDrive = windowsLikePath?.Replace(@"\\", "/").Split(":");
        var s = Path.Join(
            configHelper.GetConfig().LinuxSettings.PrefixPath,
            "dosdevices",
            $"{pathAndDrive![0].ToLower()}:", // [0] is drive letter.
            pathAndDrive[1] // [1] path to game on that drive
        );
        return s;
    }

    /// <summary>
    /// Runs an executable or Wine tool (<c>winecfg</c>, <c>winetricks</c>, <c>regedit</c>, etc.) inside the configured Wine/Proton
    /// prefix via <c>umu-run</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// RunInPrefix("EscapeFromTarkov.exe", args);           // launch any executable in the current working dir
    /// RunInPrefix("winecfg");                              // open the winecfg menu
    /// RunInPrefix("winetricks", ["-q", "win11"]);          // set the prefix's Windows version to Windows 11
    /// RunInPrefix("winetricks", ["-q", "dotnetdesktop9"]); // install .NET Desktop 9
    /// RunInPrefix("regedit");                              // open the regedit tool
    /// </code>
    /// </example>
    public bool RunInPrefix(string cmd = "", List<string>? args = null)
    {
        // This looks something like: "/home/{username}/Games/tarkov"
        // However this could be anything the user sets it too when they use MadBytes script.
        var prefixPath = configHelper.GetConfig().LinuxSettings.PrefixPath;

        // This looks something like this: "/home/{username}/.local/bin/umu-run"
        var umuPath = configHelper.GetConfig().LinuxSettings.UmuPath;

        // this looks something like this: "GE-Proton10-24"
        var proton = configHelper.GetConfig().LinuxSettings.ProtonVersion;

        // This looks something like this: "WINEDLLOVERRIDES="winhttp=n,b" ENV2=2"
        var defaultEnv = configHelper.GetConfig().LinuxSettings.DefaultEnv;

        if (string.IsNullOrEmpty(prefixPath) || string.IsNullOrEmpty(umuPath) || string.IsNullOrEmpty(proton))
        {
            logger.LogError("Prefix path or umu path or proton version are required");
            return false;
        }

        // this looks something like: "/home/{username}/Games/tarkov/drive_c/SPTarkov"
        var sptPath = configHelper.GetGamePath();

        ProcessStartInfo? process;

        // I don't know if this actually helps in any way, but some use it
        // User must install gamemode from package manager, try catch below will log it
        if (configHelper.GetConfig().LinuxSettings.GameMode)
        {
            process = new ProcessStartInfo
            {
                FileName = "gamemoderun",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = sptPath,
                Environment = { { "WINEPREFIX", prefixPath }, { "PROTONPATH", proton } },
                ArgumentList = { umuPath, cmd },
            };
        }
        else
        {
            process = new ProcessStartInfo
            {
                FileName = umuPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = sptPath,
                Environment = { { "WINEPREFIX", prefixPath }, { "PROTONPATH", proton } },
                ArgumentList = { cmd },
            };
        }

        // Add these individually so they are not wrapped in ""
        if (args != null)
        {
            foreach (var arg in args)
            {
                process.ArgumentList.Add(arg);
            }
        }

        // Combine DefaultEnv with LaunchSettings tokens
        var tokens = new List<string>();

        if (!string.IsNullOrEmpty(defaultEnv))
        {
            tokens.Add(defaultEnv);
        }

        tokens.AddRange(TokenizeLaunchSettings(configHelper.GetConfig().LinuxSettings.LaunchSettings));

        // Process all tokens with the same logic
        foreach (var token in tokens)
        {
            var separator = token.IndexOf('=');

            // args start with a -, so does anything thats not NAME=VALUE
            if (token.StartsWith('-') || separator <= 0)
            {
                process.ArgumentList.Add(token);
                continue;
            }

            // indexer not Add, a repeated name should overwrite instead of throwing
            // Remove quotes from value if present
            var value = token[(separator + 1)..].Trim('"');
            process.Environment[token[..separator]] = value;
        }

        try
        {
            Process.Start(process);
            logger.LogInformation("Game process started on linux");
        }
        catch (Exception ex)
        {
            logger.LogError("Starting game process failed: {Exception}", ex);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Splits launch options on whitespace, keeping quoted runs together so values with spaces survive, e.g.
    /// <c>MANGOHUD=1 -arg="/path with spaces" WINEDLLOVERRIDES="d3d11=n,b"</c>.
    /// </summary>
    /// <remarks>
    /// Quotes are stripped, as they only group the value - use <c>\"</c> for a literal one. Single quotes work too, and
    /// an unterminated quote just runs to the end rather than throwing.
    /// </remarks>
    internal static List<string> TokenizeLaunchSettings(string? launchSettings)
    {
        var tokens = new List<string>();

        if (string.IsNullOrWhiteSpace(launchSettings))
        {
            return tokens;
        }

        var current = new StringBuilder();
        var quote = '\0';

        // not just a length check on the builder, so an empty quoted value ("") still gives a token
        var started = false;

        for (var index = 0; index < launchSettings.Length; index++)
        {
            var character = launchSettings[index];

            if (character == '\\' && index + 1 < launchSettings.Length && launchSettings[index + 1] is '"' or '\'')
            {
                current.Append(launchSettings[++index]);
                started = true;
                continue;
            }

            if (quote != '\0')
            {
                if (character == quote)
                {
                    quote = '\0';
                }
                else
                {
                    current.Append(character);
                }

                continue;
            }

            if (character is '"' or '\'')
            {
                quote = character;
                started = true;
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (started)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                    started = false;
                }

                continue;
            }

            current.Append(character);
            started = true;
        }

        if (started)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }

    public Task<List<string>> GetProtonVersions()
    {
        // Should contain things like "GE-Proton10-24" or "GE-Proton10-21"
        // Could be named slightly different if user downloads "custom" ones like "EM-10.0-30"
        if (!Directory.Exists(Paths.ProtonPath))
        {
            logger.LogError("Proton path not found, make sure to run lutris or steam first");
            // we want this to throw an exception, so just log this
        }

        var directoryContents = Directory.GetDirectories(Paths.ProtonPath);
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

    [DllImport("libc", EntryPoint = "setenv", SetLastError = true)]
    public static extern int SetEnvironmentVariableNative(string name, string value, int overwrite);
}
