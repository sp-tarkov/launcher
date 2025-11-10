using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Helpers;
using SevenZip;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.Mods;

public class ModManager
{
    private ILogger<ModManager> _logger;
    private HttpClient _httpClient;
    private ConcurrentDictionary<string, Mod> _downloadDict = new();
    private StateHelper _stateHelper;

    public ModManager
    (
        ILogger<ModManager> logger,
        StateHelper stateHelper
    )
    {
        _logger = logger;
        _stateHelper = stateHelper;

        // leaving default atm, this will be making requests to unknown servers.
        var handler = new HttpClientHandler();
        handler.UseCookies = false;
        _httpClient = new HttpClient(handler);
    }

    public bool AddDownloadTask(string modName, string url, CancellationTokenSource token)
    {
        var taskToAdd = new Mod(modName, url, token, _httpClient);
        if (!_downloadDict.TryAdd(modName, taskToAdd))
        {
            _logger.LogWarning("Download with modName: {modName} already exists", modName);
            return false;
        }

        Task.Factory.StartNew(taskToAdd.StartModDownload);
        _stateHelper.SetHasDownloads();
        return true;
    }

    public async Task<bool> CancelDownloadTask(string modName)
    {
        if (!_downloadDict.TryRemove(modName, out var value))
        {
            _logger.LogWarning("Download with modName {modName} did not exist in Dict", modName);
            return false;
        }

        var result = await value.CancelModDownload();
        _stateHelper.SetHasDownloads();
        return result;
    }

    public bool CloseDownloadTask(string modName)
    {
        if (!_downloadDict.TryRemove(modName, out _))
        {
            _logger.LogWarning("Download with modName {modName} did not exist in Dict", modName);
            return false;
        }

        _stateHelper.SetHasDownloads();
        return true;
    }

    public ConcurrentDictionary<string, Mod> GetDownloadTasks()
    {
        return _downloadDict;
    }

    public Mod? GetDownloadTask(string modName)
    {
        if (!_downloadDict.TryGetValue(modName, out var value))
        {
            _logger.LogWarning("Download with modName: {modName} did not exist in Dict", modName);
            return null;
        }

        return value;
    }

    public async Task<bool> UnzipMod(string modName)
    {
        try
        {
            var location = Path.Combine(Paths.ModCache, modName);
            if (!File.Exists(location))
            {
                _logger.LogError("File {location} doesn't exist", location);
            }

            var extractor = new SevenZipExtractor(location);

            // TODO: do i need to check if its password protected?

            var checkForCorrectFilePath = extractor.ArchiveFileNames.Any(x =>
                !x.ToLower().Contains("bepinex") ||
                !x.ToLower().Contains("spt")
            );

            if (!checkForCorrectFilePath)
            {
                _logger.LogError("Zip does not contain a bepinex or spt folder, unsupported structure, please report to SPT staff");
                return false;
            }

            if (!Directory.Exists(Paths.UnZipped))
            {
                Directory.CreateDirectory(Paths.UnZipped);
            }

            _logger.LogInformation("Unzipping mod {modName}", modName);

            await extractor.ExtractArchiveAsync(Path.Combine(Paths.UnZipped, modName));

            var files =  Directory.GetFiles(Path.Combine(Paths.UnZipped, modName),  "*", SearchOption.AllDirectories);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception {exception}", e);
            return false;
        }

        _logger.LogInformation("Unzipped mod {modName}", modName);
        return true;
    }
}
