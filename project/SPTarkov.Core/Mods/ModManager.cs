using Microsoft.Extensions.Logging;
using SevenZip;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.Forge;
using SPTarkov.Core.SPT;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.Mods;

public class ModManager
{
    private ILogger<ModManager> _logger;
    private ConfigHelper _configHelper;
    private ModHelper _modHelper;

    public ModManager
    (
        ILogger<ModManager> logger,
        ConfigHelper configHelper,
        ModHelper modHelper
    )
    {
        _logger = logger;
        _configHelper = configHelper;
        _modHelper = modHelper;
    }

    /// <summary>
    /// TODO: add check if mod is already installed
    /// </summary>
    /// <param name="forgeMod"></param>
    /// <param name="version"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="dictOfDeps"></param>
    /// <returns></returns>
    public async Task<bool> DownloadMod(ForgeBase forgeMod, ForgeModVersion version, CancellationTokenSource cancellationToken,
        Dictionary<string, Version>? dictOfDeps = null)
    {
        dictOfDeps ??= new Dictionary<string, Version>();

        // start the download
        var downloadTask = await _modHelper.StartDownloadTask(forgeMod, version, cancellationToken);

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

        await _modHelper.RemoveModTask(downloadTask);

        configMod.Dependencies = dictOfDeps;
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
            downloadTask.CancellationTokenSource.Cancel();
            return null;
        }

        var extractor = new SevenZipExtractor(modFilePath);

        // check if zip contains bepinex or spt folder for correct starting structure
        var checkForCorrectFilePath =
            extractor.ArchiveFileNames.Any(x => x.ToLower().Contains("bepinex\\") || x.ToLower().Contains("spt\\"));

        if (!checkForCorrectFilePath)
        {
            downloadTask.Error =
                new Exception("Zip does not contain a bepinex or spt folder, unsupported structure, please report to SPT staff");
            downloadTask.CancellationTokenSource.Cancel();
            return null;
        }

        return new ConfigMod
        {
            ModName = downloadTask.ForgeMod.Name,
            ModVersion = downloadTask.Version.Version,
            GUID = downloadTask.ForgeMod.Guid,
            IsInstalled = false,
            CanBeUpdated = false,
            Files = RemoveBasePaths(extractor.ArchiveFileNames.ToList())
        };
    }

    public Dictionary<string, ConfigMod> GetMods()
    {
        return _configHelper.GetConfig().Mods;
    }

    public async Task<bool> InstallMod(string guid, CancellationTokenSource cancellationToken = null)
    {
        var modFilePath = Path.Combine(Paths.ModCache, guid);
        if (!File.Exists(modFilePath))
        {
            _logger.LogError("file not found: {file}", modFilePath);
            return false;
        }

        var configMod = GetMods().FirstOrDefault(x => x.Key == guid).Value;
        configMod.IsInstalling = true;
        _logger.LogInformation("Installing mod: {guid}", guid);

        var installTask = await _modHelper.StartInstallTask(configMod, cancellationToken);

        if (installTask == null || !installTask.Complete || installTask.Error != null)
        {
            // TODO: something fucked up, do something
            return false;
        }

        _logger.LogInformation("Installed mod: {guid}", guid);
        configMod.IsInstalling = false;
        configMod.IsInstalled = true;
        _configHelper.AddMod(configMod);

        await InstallModDependencies(guid);

        return true;
    }

    public async Task<bool> InstallModDependencies(string guid)
    {
        // get mod, get the mod deps, install mod deps if not already installed.
        var mods = GetMods();
        var mod = mods.FirstOrDefault(x => x.Key == guid); // mod to install
        var deps = mod.Value.Dependencies; // deps of mod to install
        foreach (var (depGuid, depVersion) in deps) // check if dep is installed already
        {
            // does dep exist
            if (!mods.TryGetValue(depGuid, out ConfigMod? depAsMod))
            {
                _logger.LogError("dep not found: {dep}", depGuid);
                continue;
            }

            // install it if it isnt
            if (!depAsMod.IsInstalled)
            {
                await InstallMod(depGuid);
            }
        }

        return true;
    }

    public async Task<bool> UninstallModDependencies(string guid)
    {
        // get all mods, check deps, if this mods dep is required by another mod, do not uninstall it
        // if no other mod needs it, uninstall it
        // get mod, get the mod deps, install mod deps if not already installed.
        var mods = GetMods();
        var mod = mods.FirstOrDefault(x => x.Key == guid); // mod to uninstall
        var modsToCheck = mods.Where(x => x.Key != guid).ToList();
        var deps = mod.Value.Dependencies; // deps of mod to uninstall
        foreach (var (depGuid, depVersion) in deps) // check if dep is installed already
        {
            // does dep exist
            if (!mods.TryGetValue(depGuid, out ConfigMod? depAsMod))
            {
                _logger.LogError("dep not found: {dep}", depGuid);
                continue;
            }

            // install it if it isnt
            if (depAsMod.IsInstalled)
            {
                var check = false;
                // check if other mods require that dep
                // if they do, dont uninstall
                foreach (var keyValuePair in modsToCheck)
                {
                    if (keyValuePair.Value.Dependencies.ContainsKey(depGuid))
                    {
                        // another mod requires that dep, dont remove it
                        check = true;
                    }
                }

                if (!check)
                {
                    await UninstallMod(depGuid);
                }
            }
        }

        return true;
    }

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

        // Check if there are any mods that depend on this one, if so, do not uninstall it
        var checkForDependOnThis = GetMods().Where(x => x.Value.Dependencies.ContainsKey(guid) && x.Value.IsInstalled).Any();

        if (checkForDependOnThis)
        {
            // DONT REMOVE MOD, SOMETHING DEPENDS ON IT
            // TODO: show feedback to user that this cant be uninstalled
            return false;
        }

        foreach (var file in mod.Files)
        {
            var modFilePath = Path.Combine(_configHelper.GetConfig().GamePath, file);

            // first one will likely delete most but do all to be sure
            if (Directory.Exists(modFilePath))
            {
                Directory.Delete(modFilePath, true);
            }

            // this will return false on directories
            if (File.Exists(modFilePath))
            {
                File.Delete(modFilePath);
            }
        }

        _logger.LogInformation("uninstalled mod: {guid}", guid);

        var configMod = GetMods().FirstOrDefault(x => x.Key == guid).Value;
        configMod.IsInstalled = false;

        _configHelper.AddMod(configMod);
        await UninstallModDependencies(guid);
        return true;
    }

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

        // Check if there are any mods that depend on this one, if so, do not delete it
        var checkForDependOnThis = GetMods().Where(x => x.Value.Dependencies.ContainsKey(guid)).Any();

        if (checkForDependOnThis)
        {
            // DONT REMOVE MOD, SOMETHING DEPENDS ON IT
            // TODO: show feedback to user that this cant be deleted
            return false;
        }

        foreach (var file in mod.Files)
        {
            var modFilePath = Path.Combine(_configHelper.GetConfig().GamePath, file);

            // first one will likely delete most but do all to be sure
            if (Directory.Exists(modFilePath))
            {
                Directory.Delete(modFilePath, true);
            }

            if (File.Exists(modFilePath))
            {
                File.Delete(modFilePath);
            }
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

    public async Task<bool> UpdateMod(ForgeModUpdate mod, CancellationTokenSource cancellationToken)
    {
        // copy current version to be .bak
        if (!_configHelper.GetConfig().Mods.ContainsKey(mod.CurrentVersion.GUID))
        {
            _logger.LogError("key not found: {key}", mod.CurrentVersion.GUID);
            return false;
        }

        if (!_configHelper.GetConfig().Mods.TryGetValue(mod.CurrentVersion.GUID, out var configMod))
        {
            _logger.LogError("unable to get key: {key}", mod.CurrentVersion.GUID);
            return false;
        }

        var ogPath = Path.Combine(Paths.ModCache, mod.CurrentVersion.GUID);

        File.Copy(ogPath, ogPath + ".bak", true);

        if (!File.Exists(ogPath + ".bak"))
        {
            _logger.LogError("unable to find: {file} after copy", ogPath + ".bak");
            return false;
        }

        var task = await _modHelper.StartUpdateTask(mod, cancellationToken);

        var extractor = new SevenZipExtractor(ogPath);

        // check if zip contains bepinex or spt folder for correct starting structure
        var checkForCorrectFilePath =
            extractor.ArchiveFileNames.Any(x => x.ToLower().Contains("bepinex\\") || x.ToLower().Contains("spt\\"));

        if (!checkForCorrectFilePath)
        {
            task.Error = new Exception("Zip does not contain a bepinex or spt folder, unsupported structure, please report to SPT staff");
            task.CancellationTokenSource.Cancel();
            return false;
        }

        // update config for latest version
        configMod.ModVersion = mod.RecommendedVersion.Version;
        configMod.Files = RemoveBasePaths(extractor.ArchiveFileNames.ToList());
        _configHelper.AddMod(configMod);

        // delete old zip with .bak
        File.Delete(ogPath + ".bak");

        return task.Complete;
    }

    private List<string> RemoveBasePaths(List<string> originalPaths)
    {
        return originalPaths.Where((x) =>
        {
            var lowered = x.ToLower();
            if (Paths.ArchiveFileInfoToIgnore.Contains(lowered))
            {
                return false;
            }

            return true;
        }).ToList();
    }
}
