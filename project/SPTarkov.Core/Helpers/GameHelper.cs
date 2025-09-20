using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SPTarkov.Core.Models;
using SPTarkov.Core.Patching;

namespace SPTarkov.Core.Helpers;

public class GameHelper
{
    private const string registryInstall = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\EscapeFromTarkov";
    private const string registrySettings = @"Software\Battlestate Games\EscapeFromTarkov";
    private readonly ConfigHelper _configHelper;
    private readonly ILogger<GameHelper> _logger;

    private readonly StateHelper _stateHelper;
    private FilePatcher _filePatcher;
    private string? _originalGamePath;

    private List<string> patches
    {
        get { return GetCorePatches(); }
    }

    public GameHelper(
        StateHelper stateHelper,
        ILogger<GameHelper> logger,
        ConfigHelper configHelper,
        FilePatcher filePatcher
    )
    {
        _stateHelper = stateHelper;
        _logger = logger;
        _configHelper = configHelper;
        _filePatcher = filePatcher;
        _originalGamePath = DetectOriginalGamePath();
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
        _logger.LogInformation("Launching game");
        _logger.LogInformation("account name: {acc}", _stateHelper.SelectedProfile.Username);
        _logger.LogInformation("Server: {server}", _stateHelper.SelectedServer.IpAddress);

        // setup directories
        if (IsInstalledInLive())
        {
            _logger.LogError("SPT is installed in Live");
            return false;
        }

        _logger.LogInformation("SPT is not installed in Live");

        if (IsCoreDllVersionMismatched())
        {
            _logger.LogError("Core dll mismatch");
            return false;
        }

        _logger.LogInformation("Core dll matches");

        SetupGameFiles();

        if (!Validate())
        {
            _logger.LogError("Game Validation Failed");
            return false;
        }

        _logger.LogInformation("Game Validation passed");

        try
        {
            await foreach (var patchResultInfo in PatchFiles())
            {
                var resultInfo = patchResultInfo;
            }
        }
        catch (Exception e)
        {
            _logger.LogError("patching failed: {e}", e);
            throw;
        }

        // check game path
        var clientExecutable = Path.Join(_configHelper.GetConfig().GamePath, "EscapeFromTarkov.exe");

        if (!File.Exists(clientExecutable))
        {
            _logger.LogError("Could not find {ClientExecutable}", clientExecutable);
            return false;
        }

        _logger.LogInformation("Valid game path: {ClientExecutable}", clientExecutable);


        //start game
        var args = $"-force-gfx-jobs native -token={_stateHelper.SelectedProfile.ProfileID} -config=" +
                   $"{{'BackendUrl':'https://{_stateHelper.SelectedServer.IpAddress}','Version':'live','MatchingVersion':'live'}}";

        _logger.LogInformation($"args: {args}");

        var clientProcess = new ProcessStartInfo(clientExecutable)
        {
            Arguments = args,
            UseShellExecute = false,
            WorkingDirectory = _configHelper.GetConfig().GamePath
        };

        try
        {
            Process.Start(clientProcess);
            _logger.LogInformation("Game process started");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Starting game process failed: {ex}");
            return false;
        }

        return true;
    }

    public async IAsyncEnumerable<PatchResultInfo> PatchFiles()
    {
        await foreach (var info in TryPatchFiles(false))
        {
            yield return info;

            if (info.OK)
            {
                continue;
            }

            // This will run _after_ the caller decides to continue iterating.
            await foreach (var secondInfo in TryPatchFiles(true))
            {
                yield return secondInfo;
            }

            yield break;
        }
    }

    private async IAsyncEnumerable<PatchResultInfo> TryPatchFiles(bool IgnoreInputHashMismatch)
    {
        _filePatcher.Restore(_configHelper.GetConfig().GamePath);

        int processed = 0;
        int countpatches = patches.Count;

        var _patches = patches;
        foreach (var patch in _patches)
        {
            var result =
                await Task.Factory.StartNew(() => _filePatcher.Run(_configHelper.GetConfig().GamePath, patch, IgnoreInputHashMismatch));
            if (!result.OK)
            {
                yield return new PatchResultInfo(result.Status, processed, countpatches);
                yield break;
            }

            processed++;
            var ourResult = new PatchResultInfo(PatchResultEnum.Success, processed, countpatches);
            yield return ourResult;
        }
    }

    private List<string> GetCorePatches()
    {
        return Directory.GetDirectories(Path.Combine(_configHelper.GetConfig().GamePath, "SPT_Data", "Launcher", "Patches")).ToList();
    }


    private bool IsCoreDllVersionMismatched()
    {
        try
        {
            var serverVersion = new SPTVersion("4.0.0", "123123"); // TODO: get from server via api call

            var coreDllVersionInfo = FileVersionInfo.GetVersionInfo(Path.Join(_configHelper.GetConfig().GamePath, "BepinEx", "plugins", "spt", "spt-core.dll"));
            var dllVersion = new SPTVersion(coreDllVersionInfo.FileVersion);

            _logger.LogInformation("spt-core.dll version: {DllVersion}", dllVersion);

            // Edge case, running on locally built modules dlls, ignore check and return ok
            // if (dllVersion.Major == 1)
            // {
            //     return false;
            // }
            //
            // // check 'X'.x.x
            // if (serverVersion.Major != dllVersion.Major)
            // {
            //     return true;
            // }
            //
            // // check x.'X'.x
            // if (serverVersion.Minor != dllVersion.Minor)
            // {
            //     return true;
            // }
            //
            // // check x.x.'X'
            // if (serverVersion.Build != dllVersion.Build)
            // {
            //     return true;
            // }

            return false; // Versions match, hooray
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception occured: {ex}", ex);
        }

        return true;
    }

    private bool IsInstalledInLive()
    {
        var isInstalledInLive = false;

        try
        {
            List<FileInfo> files =
            [
                // SPT files
                new(Path.Combine(_originalGamePath, "SPT.Launcher.exe")),
                new(Path.Combine(_originalGamePath, "SPT.Server.exe")),

                // bepinex files
                new(Path.Combine(_originalGamePath, "doorstep_config.ini")),
                new(Path.Combine(_originalGamePath, "winhttp.dll")),

                // licenses
                new(Path.Combine(_originalGamePath, "LICENSE-BEPINEX.txt")),
                new(Path.Combine(_originalGamePath, "LICENSE-ConfigurationManager.txt")),
                new(Path.Combine(_originalGamePath, "LICENSE-Launcher.txt")),
                new(Path.Combine(_originalGamePath, "LICENSE-Modules.txt")),
                new(Path.Combine(_originalGamePath, "LICENSE-Server.txt"))
            ];

            List<DirectoryInfo> directories =
            [
                new(Path.Combine(_originalGamePath, "SPT_Data")),
                new(Path.Combine(_originalGamePath, "BepInEx"))
            ];

            foreach (var file in files.Where(file => File.Exists(file.FullName)))
            {
                File.Delete(file.FullName);
                _logger.LogWarning("File removed - found in live dir: {FileFullName}", file.FullName);
                isInstalledInLive = true;
            }

            foreach (var directory in directories.Where(directory => Directory.Exists(directory.FullName)))
            {
                RemoveFilesRecurse(directory);
                _logger.LogWarning("Directory removed - found in live dir: {DirectoryFullName}", directory.FullName);
                isInstalledInLive = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception occured: {ex}", ex);
        }

        return isInstalledInLive;
    }


    private void SetupGameFiles()
    {
        var files = new[]
        {
            GetFileForCleanup("BattlEye"), GetFileForCleanup("Logs"), GetFileForCleanup("ConsistencyInfo"), GetFileForCleanup("EscapeFromTarkov_BE.exe"), GetFileForCleanup("Uninstall.exe"), GetFileForCleanup("UnityCrashHandler64.exe"), GetFileForCleanup("WinPixEventRuntime.dll"),

            // Don't allow excluding this from cleanup ever
            Path.Combine(_configHelper.GetConfig().GamePath, @"EscapeFromTarkov_Data\Plugins\x86_64\hwecho.dll")
        };

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
                _logger.LogInformation("Deleting file: {file}", file);
                File.Delete(file);
            }
        }
    }

    private string? GetFileForCleanup(string fileName)
    {
        if (_configHelper.GetConfig().ExcludeFromCleanup.Contains(fileName))
        {
            _logger.LogInformation("Excluded {fileName} from file cleanup", fileName);
            return null;
        }

        return Path.Join(_configHelper.GetConfig().GamePath, fileName);
    }

    /// <summary>
    /// Remove the SPT JSON-based registry keys associated with the given profile ID
    /// </summary>
    public void RemoveProfileRegistryKeys(string profileId)
    {
        var registryFile = new FileInfo(Path.Combine(Environment.CurrentDirectory, "user", "sptRegistry", "registry.json"));

        if (!registryFile.Exists)
        {
            return;
        }

        var registryData = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(registryFile.FullName));

        // Find any property that has a key containing the profileId, and remove it
        var propsToRemove = registryData.Where(prop => prop.Key.Contains(profileId, StringComparison.CurrentCultureIgnoreCase)).ToList();
        propsToRemove.ForEach(prop => registryData.Remove(prop.Key));

        File.WriteAllText(registryFile.FullName, registryData.ToString());
    }

    /// <summary>
    /// Clean the temp folder
    /// </summary>
    /// <returns>returns true if the temp folder was cleaned succefully or doesn't exist. returns false if something went wrong.</returns>
    public bool CleanTempFiles()
    {
        var rootdir = new DirectoryInfo(Path.Join(_configHelper.GetConfig().GamePath, "user", "sptappdata"));

        if (!rootdir.Exists)
        {
            return true;
        }

        return RemoveFilesRecurse(rootdir);
    }

    private bool RemoveFilesRecurse(DirectoryInfo basedir)
    {
        _logger.LogInformation($"Recursive Removal: {basedir}");

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
            _logger.LogError("Exception occured in removing files: {ex}", ex);
            return false;
        }

        return true;
    }

    public static bool Validate()
    {
        var c0 = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\EscapeFromTarkov";
        var v0 = 0;

        try
        {
            var v1 = Registry.LocalMachine.OpenSubKey(c0, false).GetValue("InstallLocation");
            var v2 = (v1 != null) ? v1.ToString() : string.Empty;
            var v3 = new DirectoryInfo(v2);
            var v4 = new FileSystemInfo[]
            {
                v3, new FileInfo(Path.Join(v2, @"BattlEye\BEClient_x64.dll")), new FileInfo(Path.Join(v2, @"BattlEye\BEService_x64.dll")), new FileInfo(Path.Join(v2, "ConsistencyInfo")), new FileInfo(Path.Join(v2, "Uninstall.exe")), new FileInfo(Path.Join(v2, "UnityCrashHandler64.exe"))
            };

            v0 = v4.Length - 1;

            foreach (var value in v4)
            {
                if (value.Exists)
                {
                    --v0;
                }
            }
        }
        catch
        {
            v0 = -1;
        }

        return v0 == 0;
    }
}
