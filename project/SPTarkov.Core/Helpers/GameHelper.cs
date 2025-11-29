using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.Patching;
using SPTarkov.Core.SPT;
using SPTarkov.Core.SPT.Responses;

namespace SPTarkov.Core.Helpers;

public class GameHelper
{
    private readonly ConfigHelper _configHelper;
    private readonly ILogger<GameHelper> _logger;
    private readonly StateHelper _stateHelper;
    private readonly HttpHelper _httpHelper;
    private readonly FilePatcher _filePatcher;
    private readonly LocaleHelper _localeHelper;
    private readonly WineHelper _wineHelper;
    private readonly ValidationUtil _validationUtil;

    private string? _originalGamePath;
    public string? ErrorMessage;

    private List<string> Patches
    {
        get { return GetCorePatches(); }
    }

    public GameHelper(
        StateHelper stateHelper,
        ILogger<GameHelper> logger,
        ConfigHelper configHelper,
        FilePatcher filePatcher,
        HttpHelper httpHelper,
        LocaleHelper localeHelper,
        WineHelper wineHelper,
        ValidationUtil validationUtil
    )
    {
        _stateHelper = stateHelper;
        _logger = logger;
        _configHelper = configHelper;
        _filePatcher = filePatcher;
        _httpHelper = httpHelper;
        _localeHelper = localeHelper;
        _wineHelper = wineHelper;
        _validationUtil = validationUtil;
        _originalGamePath = DetectOriginalGamePath();
    }

    public string? DetectOriginalGamePath()
    {
        if (OperatingSystem.IsWindows())
        {
            // We prioritize the Steam version, as the Steam CDN is faster for updates, and if someone
            // owns it on both platforms, there's a better chance their Steam version is up to date
            var steamInstallPath = _validationUtil.a();
            if (steamInstallPath != null && Path.Exists(Path.Combine(steamInstallPath, "build")))
            {
                var steamBuildDir = Path.Combine(steamInstallPath, "build");
                return Path.TrimEndingDirectorySeparator(steamBuildDir);
            }

            // Fall back to the BSG Launcher registry key if Steam isn't being used
            var uninstallStringValue = Registry.LocalMachine.OpenSubKey(Paths.UninstallEftRegKey, false)
                ?.GetValue("InstallLocation");
            var info = (uninstallStringValue is string key) ? new DirectoryInfo(key) : null;

            if (info == null)
            {
                return null;
            }

            return Path.TrimEndingDirectorySeparator(info.FullName);
        }

        // as running with linux requires wine, we can now
        if (OperatingSystem.IsLinux())
        {
            return _wineHelper.GetOriginalGamePath();
        }

        throw new Exception("Unsupported operating system");
    }

    public async Task<bool> CheckGame()
    {
        if (IsInstalledInLive())
        {
            _logger.LogError("SPT is installed in Live");
            ErrorMessage = _localeHelper.Get("game_helper_error_1");
            return false;
        }

        _logger.LogInformation("SPT is not installed in Live");

        if (await IsCoreDllVersionMismatched())
        {
            _logger.LogError("Core dll mismatch");
            ErrorMessage = _localeHelper.Get("game_helper_error_2");
            return false;
        }

        _logger.LogInformation("Core dll matches");

        SetupGameFiles();

        if (!_validationUtil.Validate())
        {
            _logger.LogError("Game Validation Failed");
            ErrorMessage = _localeHelper.Get("game_helper_error_3");
            return false;
        }

        _logger.LogInformation("Game Validation passed");

        return true;
    }

    public async Task<bool> PatchGame()
    {
        try
        {
            await foreach (var _ in PatchFiles())
            {
                // Do nothing with this
            }
        }
        catch (Exception e)
        {
            _logger.LogError("patching failed: {e}", e);
            ErrorMessage = _localeHelper.Get("game_helper_error_4");
            return false;
        }

        return true;
    }

    public async Task<bool> LaunchGame()
    {
        _logger.LogInformation("Launching game");
        _logger.LogInformation("account name: {acc}", _stateHelper.SelectedProfile?.Username);
        _logger.LogInformation("Server: {server}", _stateHelper.SelectedServer?.IpAddress);

        // check game path
        var clientExecutable = Path.Combine(_configHelper.GetConfig().GamePath, "EscapeFromTarkov.exe");

        if (!File.Exists(clientExecutable))
        {
            _logger.LogError("Could not find {ClientExecutable}", clientExecutable);
            ErrorMessage = _localeHelper.Get("game_helper_error_5");
            return false;
        }

        _logger.LogInformation("Valid game path: {ClientExecutable}", clientExecutable);

        //start game
        var args = $"-force-gfx-jobs native -token={_stateHelper.SelectedProfile?.ProfileId} -config=" +
                   $"{{'BackendUrl':'https://{_stateHelper.SelectedServer?.IpAddress}','Version':'live','MatchingVersion':'live'}}";

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
            ErrorMessage = _localeHelper.Get("game_helper_error_6");
            return false;
        }

        return true;
    }

    public async Task<bool> LaunchGameLinux()
    {
        _logger.LogInformation("Launching game on linux");
        _logger.LogInformation("account name: {acc}", _stateHelper.SelectedProfile?.Username);
        _logger.LogInformation("Server: {server}", _stateHelper.SelectedServer?.IpAddress);

        List<string> argsList =
        [
            "-force-gfx-jobs",
            "native",
            $"-token={_stateHelper.SelectedProfile?.ProfileId}",
            $"-config={{'BackendUrl':'https://{_stateHelper.SelectedServer?.IpAddress}','Version':'live','MatchingVersion':'live'}}"
        ];

        if (!await _wineHelper.RunInPrefix("EscapeFromTarkov.exe", argsList))
        {
            return false;
        }

        return true;
    }

    private async IAsyncEnumerable<PatchResultInfo> PatchFiles()
    {
        await foreach (var info in TryPatchFiles(false))
        {
            yield return info;

            if (info.Ok)
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

    private async IAsyncEnumerable<PatchResultInfo> TryPatchFiles(bool ignoreInputHashMismatch)
    {
        _filePatcher.Restore(_configHelper.GetConfig().GamePath);

        var processed = 0;
        var countpatches = Patches.Count;

        foreach (var patch in Patches)
        {
            var result = await Task.Factory.StartNew(() => _filePatcher.Run(_configHelper.GetConfig().GamePath, patch, ignoreInputHashMismatch));
            if (!result.Ok)
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
        return Directory.GetDirectories(Path.Combine(_configHelper.GetConfig().GamePath, Paths.PatchPath)).ToList();
    }

    private async Task<bool> IsCoreDllVersionMismatched()
    {
        try
        {
            var call = await _httpHelper.GameServerGet<SPTVersionResponse>(Urls.Version, CancellationToken.None);

            var serverVersion = new SptVersion(call?.Response!);
            var coreDllPath = Path.Combine(_configHelper.GetConfig().GamePath, Paths.CoreDllPath);
            if (!File.Exists(coreDllPath))
            {
                _logger.LogError("spt-core.dll missing: {coreDllPath}", coreDllPath);
            }

            var coreDllVersionInfo = FileVersionInfo.GetVersionInfo(coreDllPath);
            var dllVersion = new SptVersion(coreDllVersionInfo.FileVersion!);

            _logger.LogInformation("server version: {serverVersion} - spt-core.dll version: {DllVersion}", serverVersion, dllVersion);

            // Edge case, running on locally built modules dlls, ignore check and return ok
            if (dllVersion.Major == 1)
            {
                return false;
            }

            // check 'X'.x.x
            if (serverVersion.Major != dllVersion.Major)
            {
                return true;
            }

            // check x.'X'.x
            if (serverVersion.Minor != dllVersion.Minor)
            {
                return true;
            }

            // check x.x.'X'
            if (serverVersion.Build != dllVersion.Build)
            {
                return true;
            }

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
                new(Path.Combine(_originalGamePath!, "SPT.Launcher.exe")),
                new(Path.Combine(_originalGamePath!, "SPT.Server.exe")),

                // bepinex files
                new(Path.Combine(_originalGamePath!, "doorstep_config.ini")),
                new(Path.Combine(_originalGamePath!, "winhttp.dll")),

                // licenses
                new(Path.Combine(_originalGamePath!, "LICENSE-BEPINEX.txt")),
                new(Path.Combine(_originalGamePath!, "LICENSE-ConfigurationManager.txt")),
                new(Path.Combine(_originalGamePath!, "LICENSE-Launcher.txt")),
                new(Path.Combine(_originalGamePath!, "LICENSE-Modules.txt")),
                new(Path.Combine(_originalGamePath!, "LICENSE-Server.txt"))
            ];

            List<DirectoryInfo> directories =
            [
                new(Path.Combine(_originalGamePath!, "SPT_Data")),
                new(Path.Combine(_originalGamePath!, "BepInEx"))
            ];

            foreach (var file in files.Where(file => File.Exists(file.FullName)))
            {
                File.Delete(file.FullName);
                _logger.LogWarning("File removed - found in live dir: {FileFullName}", file.FullName);
                isInstalledInLive = true;
            }

            foreach (var directory in directories.Where(directory => Directory.Exists(directory.FullName)))
            {
                if (!TryRemoveFilesRecursively(directory))
                {
                    _logger.LogWarning("Directory removal failed - found in live dir: {DirectoryFullName}", directory.FullName);
                }

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
            GetFileForCleanup("BattlEye"),
            GetFileForCleanup("Logs"),
            GetFileForCleanup("ConsistencyInfo"),
            GetFileForCleanup("EscapeFromTarkov_BE.exe"),
            GetFileForCleanup("Uninstall.exe"),
            GetFileForCleanup("UnityCrashHandler64.exe"),
            GetFileForCleanup("WinPixEventRuntime.dll"),

            // Don't allow excluding this from cleanup ever
            Path.Combine(_configHelper.GetConfig().GamePath, Paths.HwechoDllPath)
        };

        foreach (var file in files)
        {
            if (file == null)
            {
                continue;
            }

            if (Directory.Exists(file))
            {
                TryRemoveFilesRecursively(new DirectoryInfo(file));
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

        return Path.Combine(_configHelper.GetConfig().GamePath, fileName);
    }

    /// <summary>
    /// Remove the SPT JSON-based registry keys associated with the given profile ID
    /// </summary>
    public void RemoveProfileRegistryKeys(string profileId)
    {
        var registryFile = new FileInfo(Path.Combine(Environment.CurrentDirectory, Paths.SptRegJson));

        if (!registryFile.Exists)
        {
            return;
        }

        var registryData = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(registryFile.FullName));

        // Find any property that has a key containing the profileId, and remove it
        var propsToRemove = registryData?.Where(prop => prop.Key.Contains(profileId, StringComparison.CurrentCultureIgnoreCase)).ToList();
        propsToRemove?.ForEach(prop => registryData?.Remove(prop.Key));

        File.WriteAllText(registryFile.FullName, registryData?.ToString());
    }

    private bool TryRemoveFilesRecursively(DirectoryInfo basedir)
    {
        _logger.LogInformation("Recursive Removal: {DirectoryInfo}", basedir);

        if (!basedir.Exists)
        {
            return true;
        }

        try
        {
            // remove subdirectories
            foreach (var dir in basedir.EnumerateDirectories())
            {
                TryRemoveFilesRecursively(dir);
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

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public async Task<bool> MonitorGame()
    {
        // if (!OperatingSystem.IsWindows())
        // {
        //     return false;
        // }

        // As wine can take some time to start the game, we'll just delay 12seconds,
        await Task.Delay(12000);
        var process = Process.GetProcessesByName("EscapeFromTarko").FirstOrDefault();

        if (process != null)
        {
            while (!process.HasExited)
            {
                await Task.Delay(3000);
            }
        }

        return false;
    }
}
