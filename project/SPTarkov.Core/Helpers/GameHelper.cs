using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.Patching;
using SPTarkov.Core.SPT;
using SPTarkov.Core.SPT.Responses;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.Helpers;

public class GameHelper
{
    private readonly ConfigHelper _configHelper;
    private readonly ILogger<GameHelper> _logger;
    private readonly StateHelper _stateHelper;
    private readonly HttpHelper _httpHelper;
    private readonly FilePatcher _filePatcher;
    private readonly LocaleHelper _localeHelper;
    private readonly LinuxHelper _linuxHelper;

    public string? ErrorMessage;

    private List<string> Patches
    {
        get { return Directory.GetDirectories(Paths.PatchPath).ToList(); }
    }

    public GameHelper(
        StateHelper stateHelper,
        ILogger<GameHelper> logger,
        ConfigHelper configHelper,
        FilePatcher filePatcher,
        HttpHelper httpHelper,
        LocaleHelper localeHelper,
        LinuxHelper linuxHelper
    )
    {
        _stateHelper = stateHelper;
        _logger = logger;
        _configHelper = configHelper;
        _filePatcher = filePatcher;
        _httpHelper = httpHelper;
        _localeHelper = localeHelper;
        _linuxHelper = linuxHelper;
    }

    public async Task<bool> CheckGame()
    {
        if (await IsCoreDllVersionMismatched())
        {
            return false;
        }

        _logger.LogInformation("Core dll matches");

        SetupGameFiles();

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

    public bool LaunchGame()
    {
        _logger.LogInformation("Launching game");
        _logger.LogInformation("account name: {acc}", _stateHelper.SelectedProfile?.Username);
        _logger.LogInformation("Server: {server}", _stateHelper.SelectedServer?.IpAddress);

        // check game path
        var clientExecutable = Path.Join(_configHelper.GetGamePath(), "EscapeFromTarkov.exe");

        if (!File.Exists(clientExecutable))
        {
            _logger.LogError("Could not find {ClientExecutable}", clientExecutable);
            ErrorMessage = _localeHelper.Get("game_helper_error_5");
            return false;
        }

        _logger.LogInformation("Valid game path: {ClientExecutable}", clientExecutable);

        // Start game
        var args =
            $"-force-gfx-jobs native -token={_stateHelper.SelectedProfile?.ProfileId} -config="
            + $"{{'BackendUrl':'https://{_stateHelper.SelectedServer?.IpAddress}','Version':'live','MatchingVersion':'live'}}";

        _logger.LogInformation("args: {Args}", args);

        var clientProcess = new ProcessStartInfo(clientExecutable)
        {
            Arguments = args,
            UseShellExecute = false,
            WorkingDirectory = _configHelper.GetGamePath(),
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

    public bool LaunchGameLinux()
    {
        _logger.LogInformation("Launching game on linux");
        _logger.LogInformation("account name: {acc}", _stateHelper.SelectedProfile?.Username);
        _logger.LogInformation("Server: {server}", _stateHelper.SelectedServer?.IpAddress);

        List<string> argsList =
        [
            "-force-gfx-jobs",
            "native",
            $"-token={_stateHelper.SelectedProfile?.ProfileId}",
            $"-config={{'BackendUrl':'https://{_stateHelper.SelectedServer?.IpAddress}','Version':'live','MatchingVersion':'live'}}",
        ];

        if (!_linuxHelper.RunInPrefix("EscapeFromTarkov.exe", argsList))
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
        _filePatcher.Restore(_configHelper.GetGamePath());

        var processed = 0;
        var countpatches = Patches.Count;

        foreach (var patch in Patches)
        {
            var result = await _filePatcher.Run(_configHelper.GetGamePath(), patch, ignoreInputHashMismatch);
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

    private async Task<bool> IsCoreDllVersionMismatched()
    {
        try
        {
            var call = await _httpHelper.GameServerGet<SPTVersionResponse>(Urls.Version, CancellationToken.None);

            // Server returns "X.Y.Z" (release) or "X.Y.Z - <bleeding edge text>" (dev build); take the numeric core.
            var serverVersion = new Version(call?.Response!.Split('-')[0].Trim());
            var coreDllPath = Path.Join(_configHelper.GetGamePath(), Paths.CoreDllPath);
            if (!File.Exists(coreDllPath))
            {
                _logger.LogError("spt-core.dll missing: {coreDllPath}", coreDllPath);
            }

            // Read the DLL's numeric parts directly; its FileVersion may carry a 4th (revision) segment that SemVer rejects.
            var coreDllVersionInfo = FileVersionInfo.GetVersionInfo(coreDllPath);
            var dllVersion = new Version(
                coreDllVersionInfo.FileMajorPart,
                coreDllVersionInfo.FileMinorPart,
                coreDllVersionInfo.FileBuildPart
            );

            _logger.LogInformation("server version: {serverVersion} - spt-core.dll version: {DllVersion}", serverVersion, dllVersion);

            // Edge case, running on locally built modules dlls, ignore check and return ok
            if (
                dllVersion.Major != 1
                && serverVersion.Major == dllVersion.Major
                && serverVersion.Minor == dllVersion.Minor
                && serverVersion.Patch == dllVersion.Patch
            )
            {
                return false; // Versions match, hooray
            }

            _logger.LogError("Core dll mismatch");
            ErrorMessage = _localeHelper.Get("game_helper_error_2");
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("Connection lost/refused: {ex}", ex);
            ErrorMessage = _localeHelper.Get("game_helper_error_7");
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception occured: {ex}", ex);
            ErrorMessage = _localeHelper.Get("game_helper_error_8");
        }

        return true;
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
            Path.Join(_configHelper.GetGamePath(), Paths.HwechoDllPath),
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

        return Path.Join(_configHelper.GetGamePath(), fileName);
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
        // As wine can take some time to start the game, we'll just delay 12seconds,
        // needs to search for EscapeFromTarkov for windows
        // TODO: this might not work for linux
        await Task.Delay(12000);
        var process = Process.GetProcessesByName("EscapeFromTarkov").FirstOrDefault();

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
