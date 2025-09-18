using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SPTarkov.Core.Helpers;

public class GameHelper
{
    private const string registryInstall = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\EscapeFromTarkov";
    private const string registrySettings = @"Software\Battlestate Games\EscapeFromTarkov";
    private readonly ConfigHelper _configHelper;
    private readonly LogHelper _logHelper;

    private readonly StateHelper _stateHelper;

    public GameHelper(
        StateHelper stateHelper,
        LogHelper logHelper,
        ConfigHelper configHelper
    )
    {
        _stateHelper = stateHelper;
        _logHelper = logHelper;
        _configHelper = configHelper;
    }

    private string? DetectOriginalGamePath()
    {
        // We can't detect the installed path on non-Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        var installLocation = Registry.LocalMachine.OpenSubKey(registryInstall, false)?.GetValue("InstallLocation");
        var info = installLocation is string key ? new DirectoryInfo(key) : null;
        return info?.FullName;
    }

    public async Task<bool> LaunchGame()
    {
        // setup directories
        // if (IsInstalledInLive())
        // {
        //     return false;
        // }
        //
        // SetupGameFiles();

        // check game path
        var clientExecutable = Path.Join(_configHelper.GetConfig().GamePath, "EscapeFromTarkov.exe");

        if (!File.Exists(clientExecutable))
        {
            _logHelper.AddLog("[LaunchGame] Valid Game Path   :: FAILED");
            _logHelper.AddLog($"Could not find {clientExecutable}");
            return false;
        }

        // apply patches
        // TODO: set up patching

        //start game
        var args =
            $"-force-gfx-jobs native -token={_stateHelper.SelectedProfile.ProfileID} -config=" +
            $"{{'BackendUrl':'https://{_stateHelper.SelectedServer.IpAddress}','Version':'live','MatchingVersion':'live'}}";

        _logHelper.AddLog(args);

        var clientProcess = new ProcessStartInfo(clientExecutable)
        {
            Arguments = args,
            UseShellExecute = false,
            WorkingDirectory = _configHelper.GetConfig().GamePath
        };

        try
        {
            Process.Start(clientProcess);
            _logHelper.AddLog("[LaunchGame] Game process started");
        }
        catch (Exception ex)
        {
            _logHelper.AddLog(ex.ToString());
            return false;
        }

        return true;
    }

    private bool IsInstalledInLive()
    {
        var isInstalledInLive = false;

        try
        {
            FileInfo[] files =
            [
                // SPT files
                new(Path.Combine(_configHelper.GetConfig().GamePath, "SPT.Launcher.exe")),
                new(Path.Combine(_configHelper.GetConfig().GamePath, "SPT.Server.exe")),

                // bepinex files
                new(Path.Combine(_configHelper.GetConfig().GamePath, @"doorstep_config.ini")),
                new(Path.Combine(_configHelper.GetConfig().GamePath, @"winhttp.dll")),

                // licenses
                new(Path.Combine(_configHelper.GetConfig().GamePath, @"LICENSE-BEPINEX.txt")),
                new(Path.Combine(_configHelper.GetConfig().GamePath, @"LICENSE-ConfigurationManager.txt")),
                new(Path.Combine(_configHelper.GetConfig().GamePath, @"LICENSE-Launcher.txt")),
                new(Path.Combine(_configHelper.GetConfig().GamePath, @"LICENSE-Modules.txt")),
                new(Path.Combine(_configHelper.GetConfig().GamePath, @"LICENSE-Server.txt"))
            ];
            DirectoryInfo[] directories =
            [
                new(Path.Combine(_configHelper.GetConfig().GamePath, @"SPT_Data")),
                new(Path.Combine(_configHelper.GetConfig().GamePath, @"BepInEx"))
            ];

            foreach (var file in files)
            {
                if (!File.Exists(file.FullName))
                {
                    continue;
                }

                File.Delete(file.FullName);

                isInstalledInLive = true;
            }

            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory.FullName))
                {
                    continue;
                }

                RemoveFilesRecurse(directory);

                isInstalledInLive = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return isInstalledInLive;
    }


    private void SetupGameFiles()
    {
        var files = new[] { GetFileForCleanup("BattlEye"), GetFileForCleanup("Logs"), GetFileForCleanup("ConsistencyInfo"), GetFileForCleanup("EscapeFromTarkov_BE.exe"), GetFileForCleanup("Uninstall.exe"), GetFileForCleanup("UnityCrashHandler64.exe"), GetFileForCleanup("WinPixEventRuntime.dll") };

        foreach (var file in files)
        {
            if (file == null)
            {
                continue;
            }

            if (Directory.Exists(file))
            {
                RemoveFilesRecurse(new DirectoryInfo(file));
            }

            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    private string GetFileForCleanup(string fileName)
    {
        //!_excludeFromCleanup.Contains(fileName) ? Path.Combine(gamePath, fileName) : null
        return Path.Join(_configHelper.GetConfig().GamePath, fileName);
    }

    /// <summary>
    ///     Clean the temp folder
    /// </summary>
    /// <returns>returns true if the temp folder was cleaned succefully or doesn't exist. returns false if something went wrong.</returns>
    public bool CleanTempFiles()
    {
        var rootdir = new DirectoryInfo(Path.Join(_configHelper.GetConfig().GamePath, "user\\sptappdata"));

        return !rootdir.Exists || RemoveFilesRecurse(rootdir);
    }

    private bool RemoveFilesRecurse(DirectoryInfo basedir)
    {
        if (!basedir.Exists)
        {
            return true;
        }

        try
        {
            // remove subdirectories
            foreach (var dir in basedir.EnumerateDirectories())
            {
                RemoveFilesRecurse(dir);
            }

            // remove files
            var files = basedir.GetFiles();

            foreach (var file in files)
            {
                file.IsReadOnly = false;
                file.Delete();
            }

            // remove directory
            basedir.Delete();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }

        return true;
    }
}
