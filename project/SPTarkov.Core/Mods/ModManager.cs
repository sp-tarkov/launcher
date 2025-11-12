using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Helpers;
using SevenZip;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.Forge;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.Mods;

public class ModManager
{
    private ILogger<ModManager> _logger;
    private ConfigHelper _configHelper;
    private DownloadHelper _downloadHelper;

    public ModManager
    (
        ILogger<ModManager> logger,
        ConfigHelper configHelper,
        DownloadHelper downloadHelper
    )
    {
        _logger = logger;
        _configHelper = configHelper;
        _downloadHelper = downloadHelper;
        // LoadMods();
    }

    public async Task<bool> DownloadMod(ForgeBase forgeMod, ForgeModVersion version, CancellationTokenSource cancellationToken)
    {
        // start the download
        var downloadTask = await _downloadHelper.StartDownloadTask(forgeMod, version, cancellationToken);

        if (!downloadTask.Complete)
        {
            _logger.LogError("Download task failed for mod {mod}: {e}", forgeMod.Name, downloadTask.Error);
            return false;
        }

        var configMod = await ConvertToConfigMod(downloadTask);

        if (configMod == null)
        {
            _logger.LogError("configMod is null, download task: {downloadTask}", downloadTask.ToString());
            return false;
        }

        await _downloadHelper.RemoveDownloadTask(downloadTask);
        _configHelper.AddMod(configMod);

        _logger.LogDebug("Download task completed");
        return true;
    }

    private async Task<ConfigMod?> ConvertToConfigMod(DownloadTask downloadTask)
    {
        var modFilePath = Path.Combine(Paths.ModCache, downloadTask.ForgeMod.Guid);
        if (!File.Exists(modFilePath))
        {
            downloadTask.Error = new FileNotFoundException("file not found", modFilePath);
            downloadTask.CancellationToken.Cancel();
            return null;
        }

        var extractor = new SevenZipExtractor(modFilePath);

        var checkForCorrectFilePath = extractor.ArchiveFileNames.Any(x =>
            !x.ToLower().Contains("bepinex") ||
            !x.ToLower().Contains("spt")
        );

        if (!checkForCorrectFilePath)
        {
            downloadTask.Error = new Exception("Zip does not contain a bepinex or spt folder, unsupported structure, please report to SPT staff");
            return null;
        }

        var files = extractor.ArchiveFileNames.ToList();

        foreach (var file in files)
        {
            Console.WriteLine(file);
        }

        return new ConfigMod
        {
            ModName = downloadTask.ForgeMod.Name,
            GUID = downloadTask.ForgeMod.Guid,
            IsInstalled = false,
            CanBeUpdated = false,
            Files = files
        };
    }

    public Dictionary<string, ConfigMod> GetMods()
    {
        return _configHelper.GetConfig().Mods;
    }
}
