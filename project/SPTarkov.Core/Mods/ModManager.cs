using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MudBlazor;
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
            _logger.LogError("configMod is null, download error: {downloadTask}", downloadTask.Error);
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

        var checkForCorrectFilePath = extractor.ArchiveFileNames.All(x =>
            !x.ToLower().Contains("bepinex") ||
            !x.ToLower().Contains("spt")
        );

        if (checkForCorrectFilePath)
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

    public async Task<bool> InstallMod(string guid)
    {
        var modFilePath = Path.Combine(Paths.ModCache, guid);
        if (!File.Exists(modFilePath))
        {
            _logger.LogError("file not found: {file}", modFilePath);
            return false;
        }

        var extractor = new SevenZipExtractor(modFilePath);

        // com.skitles.profile.editor this should have failed.
        var checkForCorrectFilePath = extractor.ArchiveFileNames.Any(x =>
            !x.ToLower().Contains("bepinex") ||
            !x.ToLower().Contains("spt")
        );

        // we checked this before, but to be sure
        if (!checkForCorrectFilePath)
        {
            _logger.LogError("Zip does not contain a bepinex or spt folder, unsupported structure, please report to SPT staff");
            return false;
        }

        await extractor.ExtractArchiveAsync(_configHelper.GetConfig().GamePath);
        _logger.LogInformation("Installed mod: {guid}", guid);

        var configMod = GetMods().FirstOrDefault(x => x.Key == guid).Value;
        configMod.IsInstalled = true;

        _configHelper.AddMod(configMod);
        return true;
    }

    /// <summary>
    /// TODO: folders are risidual, and configs dont get saved. if its even worth doing
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public async Task<bool> UninstallMod(string guid)
    {
        if (!_configHelper.GetConfig().Mods.ContainsKey(guid))
        {
            _logger.LogError("key not found: {key}", guid);
            return false;
        }

        if (!_configHelper.GetConfig().Mods.TryGetValue(guid, out var mod))
        {
            _logger.LogError("unable to get key: {key}", guid);
            return false;
        }

        foreach (var file in mod.Files)
        {
            var modFilePath = Path.Combine(_configHelper.GetConfig().GamePath, file);
            // if this is a directory, it should return false
            if (!File.Exists(modFilePath))
            {
                continue;
            }

            File.Delete(modFilePath);
        }

        _logger.LogInformation("uninstalled mod: {guid}", guid);

        var configMod = GetMods().FirstOrDefault(x => x.Key == guid).Value;
        configMod.IsInstalled = false;

        _configHelper.AddMod(configMod);
        return true;
    }

    /// <summary>
    /// TODO: folders are risidual, and configs dont get saved. if its even worth doing
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public async Task<bool> DeleteMod(string guid)
    {
        if (!_configHelper.GetConfig().Mods.ContainsKey(guid))
        {
            _logger.LogError("key not found: {key}", guid);
            return false;
        }

        if (!_configHelper.GetConfig().Mods.TryGetValue(guid, out var mod))
        {
            _logger.LogError("unable to get key: {key}", guid);
            return false;
        }

        foreach (var file in mod.Files)
        {
            var modFilePath = Path.Combine(_configHelper.GetConfig().GamePath, file);
            // if this is a directory, it should return false
            if (!File.Exists(modFilePath))
            {
                continue;
            }

            File.Delete(modFilePath);
        }

        _logger.LogInformation("Deleted mod: {guid}", guid);

        if (File.Exists(Path.Combine(Paths.ModCache, guid)))
        {
            _logger.LogInformation("deleted zip for mod {guid}", guid);
            File.Delete(Path.Combine(Paths.ModCache, guid));
        }

        _configHelper.RemoveMod(guid);
        return true;
    }
}
