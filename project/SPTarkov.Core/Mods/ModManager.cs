using Microsoft.Extensions.Logging;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.Forge;
using SPTarkov.Core.SPT;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.Mods;

public class ModManager(
    ILogger<ModManager> logger,
    ConfigHelper configHelper,
    ModHelper modHelper,
    SevenZip.SevenZip sevenZip)
{
    /// <summary>
    /// TODO: add check if mod is already installed
    /// </summary>
    /// <param name="forgeMod"></param>
    /// <param name="version"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="dictOfDeps"></param>
    /// <returns></returns>
    public async Task DownloadMod(ForgeBase forgeMod, ForgeModVersion version, CancellationToken cancellationToken = default,
        Dictionary<string, Version>? dictOfDeps = null)
    {
        dictOfDeps ??= new Dictionary<string, Version>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // start the download
        var downloadTask = await modHelper.StartDownloadTask(forgeMod, version, cts);

        if (downloadTask == null)
        {
            logger.LogError("Download task failed for mod {mod}: {e}", forgeMod.Name, downloadTask.Error);
        }

        if (!downloadTask.Complete)
        {
            logger.LogError("Download task failed for mod {mod}: {e}", forgeMod.Name, downloadTask.Error);
            return;
        }

        var configMod = await ConvertToConfigMod(downloadTask);

        if (configMod == null)
        {
            logger.LogError("configMod is null, download error: {downloadTask}", downloadTask.Error);
            return;
        }

        modHelper.RemoveModTask(downloadTask);

        configMod.Dependencies = dictOfDeps;
        configHelper.AddMod(configMod);

        logger.LogDebug("Download task completed");
    }

    private async Task<ConfigMod?> ConvertToConfigMod(DownloadTask downloadTask)
    {
        var modFilePath = Path.Join(Paths.ModCache, downloadTask.ForgeMod.Guid);
        if (!File.Exists(modFilePath))
        {
            downloadTask.Error = new FileNotFoundException("file not found", modFilePath);
            await downloadTask.CancellationTokenSource.CancelAsync();
            return null;
        }

        var entries = await sevenZip.GetEntriesAsync(modFilePath, downloadTask.CancellationTokenSource.Token);

        // check if zip contains bepinex or spt folder for correct starting structure
        // this should be bepinex\ on windows and bepinex/ on linux
        var checkForCorrectFilePath = entries.Any(x =>
            x.ToLower().Contains("bepinex" + Path.DirectorySeparatorChar) || x.ToLower().Contains("spt" + Path.DirectorySeparatorChar));

        if (!checkForCorrectFilePath)
        {
            downloadTask.Error =
                new Exception("Zip does not contain a bepinex or spt folder, unsupported structure, please report to SPT staff");
            await downloadTask.CancellationTokenSource.CancelAsync();
            return null;
        }

        return new ConfigMod
        {
            ModName = downloadTask.ForgeMod.Name,
            ModVersion = downloadTask.Version.Version,
            GUID = downloadTask.ForgeMod.Guid,
            IsInstalled = false,
            CanBeUpdated = false,
            Files = RemoveBasePaths(entries)
        };
    }

    public Dictionary<string, ConfigMod> GetMods()
    {
        return configHelper.GetConfig().Mods;
    }

    public async Task<bool> InstallMod(string guid, CancellationToken cancellationToken = default)
    {
        var modFilePath = Path.Join(Paths.ModCache, guid);
        if (!File.Exists(modFilePath))
        {
            logger.LogError("file not found: {file}", modFilePath);
            return false;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var configMod = GetMods().FirstOrDefault(x => x.Key == guid).Value;
        configMod.IsInstalling = true;
        logger.LogInformation("Installing mod: {guid}", guid);

        try
        {
            var installTask = await modHelper.StartInstallTask(configMod, cts);

            if (installTask == null || !installTask.Complete || installTask.Error != null)
            {
                // TODO: something fucked up, do something or cancelled
                logger.LogError("install task failed for mod {mod}: {e}", guid, installTask?.Error);
                configMod.IsInstalling = false;
                return false;
            }

            modHelper.RemoveModTask(installTask);
        }
        catch (Exception e)
        {
            logger.LogWarning("install task failed for reason:  {reason}", e.Message);
            configMod.IsInstalling = false;
            return false;
        }

        logger.LogInformation("Installed mod: {guid}", guid);
        configMod.IsInstalling = false;
        configMod.IsInstalled = true;
        configHelper.AddMod(configMod);

        await InstallModDependencies(guid);

        return true;
    }

    public async Task InstallModDependencies(string guid)
    {
        // get mod, get the mod deps, install mod deps if not already installed.
        var mods = GetMods();
        var mod = mods.FirstOrDefault(x => x.Key == guid); // mod to install
        var deps = mod.Value.Dependencies; // deps of mod to install

        if (deps == null)
        {
            return;
        }

        foreach (var (depGuid, _) in deps) // check if dep is installed already
        {
            // does dep exist
            if (!mods.TryGetValue(depGuid, out var depAsMod))
            {
                logger.LogError("dep not found: {dep}", depGuid);
                continue;
            }

            // Install it if it isn't
            if (!depAsMod.IsInstalled)
            {
                await InstallMod(depGuid);
            }
        }
    }

    public async Task UninstallModDependencies(string guid)
    {
        // get all mods, check deps, if this mods dep is required by another mod, do not uninstall it
        // if no other mod needs it, uninstall it
        // get mod, get the mod deps, install mod deps if not already installed.
        var mods = GetMods();
        var mod = mods.FirstOrDefault(x => x.Key == guid); // mod to uninstall
        var modsToCheck = mods.Where(x => x.Key != guid).ToList();
        var deps = mod.Value.Dependencies; // deps of mod to uninstall

        if (deps == null)
        {
            return;
        }

        foreach (var (depGuid, _) in deps) // check if dep is installed already
        {
            // does dep exist
            if (!mods.TryGetValue(depGuid, out var depAsMod))
            {
                logger.LogError("dep not found: {dep}", depGuid);
                continue;
            }

            if (depAsMod.IsInstalled)
            {
                var check = false;

                // check if other mods require that dep
                // if they do, don't uninstall
                foreach (var keyValuePair in modsToCheck)
                {
                    if (keyValuePair.Value.Dependencies != null && keyValuePair.Value.Dependencies.ContainsKey(depGuid))
                    {
                        // another mod requires that dep, don't remove it
                        check = true;
                    }
                }

                if (!check)
                {
                    await UninstallMod(depGuid);
                }
            }
        }
    }

    public async Task<bool> UninstallMod(string guid)
    {
        if (!configHelper.GetConfig().Mods.ContainsKey(guid))
        {
            logger.LogError("key not found: {key}", guid);
            return false;
        }

        if (!configHelper.GetConfig().Mods.TryGetValue(guid, out var mod))
        {
            logger.LogError("unable to get key: {key}", guid);
            return false;
        }

        // Check if there are any mods that depend on this one, if so, do not uninstall it
        var checkForDependOnThis =
            GetMods().Any(x => x.Value.Dependencies != null && x.Value.Dependencies.ContainsKey(guid) && x.Value.IsInstalled);

        if (checkForDependOnThis)
        {
            // DONT REMOVE MOD, SOMETHING DEPENDS ON IT
            // TODO: show feedback to user that this cant be uninstalled
            return false;
        }

        if (mod.Files != null)
        {
            foreach (var modFilePath in mod.Files.Select(file => Path.Join(configHelper.GetConfig().GamePath, file)))
            {
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
        }

        logger.LogInformation("uninstalled mod: {guid}", guid);

        var configMod = GetMods().FirstOrDefault(x => x.Key == guid).Value;
        configMod.IsInstalled = false;

        configHelper.AddMod(configMod);
        await UninstallModDependencies(guid);

        return true;
    }

    public void DeleteMod(string guid)
    {
        if (!configHelper.GetConfig().Mods.ContainsKey(guid))
        {
            logger.LogError("key not found: {key}", guid);
            return;
        }

        if (!configHelper.GetConfig().Mods.TryGetValue(guid, out var mod))
        {
            logger.LogError("unable to get key: {key}", guid);
            return;
        }

        // Check if there are any mods that depend on this one, if so, do not delete it
        var checkForDependOnThis = GetMods().Any(x => x.Value.Dependencies != null && x.Value.Dependencies.ContainsKey(guid));

        if (checkForDependOnThis)
        {
            // DONT REMOVE MOD, SOMETHING DEPENDS ON IT
            // TODO: show feedback to user that this cant be deleted
            return;
        }

        if (mod.Files != null)
        {
            foreach (var modFilePath in mod.Files.Select(file => Path.Join(configHelper.GetConfig().GamePath, file)))
            {
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
        }

        logger.LogInformation("Deleted mod: {guid}", guid);

        if (File.Exists(Path.Join(Paths.ModCache, guid)))
        {
            logger.LogInformation("deleted zip for mod {guid}", guid);
            File.Delete(Path.Join(Paths.ModCache, guid));
        }

        configHelper.RemoveMod(guid);
    }

    public async Task UpdateMod(ForgeModUpdate mod, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // copy current version to be .bak
        if (!configHelper.GetConfig().Mods.ContainsKey(mod.CurrentVersion.GUID))
        {
            logger.LogError("key not found: {key}", mod.CurrentVersion.GUID);
            return;
        }

        if (!configHelper.GetConfig().Mods.TryGetValue(mod.CurrentVersion.GUID, out var configMod))
        {
            logger.LogError("unable to get key: {key}", mod.CurrentVersion.GUID);
            return;
        }

        var ogPath = Path.Join(Paths.ModCache, mod.CurrentVersion.GUID);

        File.Copy(ogPath, ogPath + ".bak", true);

        if (!File.Exists(ogPath + ".bak"))
        {
            logger.LogError("unable to find: {file} after copy", ogPath + ".bak");
            return;
        }

        var updateTask = await modHelper.StartUpdateTask(mod, cts);

        var entries = await sevenZip.GetEntriesAsync(ogPath, cts.Token);

        // check if zip contains bepinex or spt folder for correct starting structure
        // this should be bepinex\ on Windows and bepinex/ on linux
        var checkForCorrectFilePath = entries.Any(x =>
            x.ToLower().Contains("bepinex" + Path.DirectorySeparatorChar) || x.ToLower().Contains("spt" + Path.DirectorySeparatorChar));

        if (!checkForCorrectFilePath)
        {
            updateTask.Error =
                new Exception("Zip does not contain a bepinex or spt folder, unsupported structure, please report to SPT staff");
            await cts.CancelAsync();
            return;
        }

        // update config for latest version
        configMod.ModVersion = mod.RecommendedVersion.Version;
        configMod.Files = RemoveBasePaths(entries);
        configHelper.AddMod(configMod);

        // delete old zip with .bak
        File.Delete(ogPath + ".bak");

        modHelper.RemoveModTask(updateTask!);
    }

    private List<string> RemoveBasePaths(List<string> originalPaths)
    {
        return originalPaths.Where(x =>
        {
            var lowered = x.ToLower();
            if (Paths.ArchiveFileInfoToIgnore.Contains(lowered))
            {
                return false;
            }

            return true;
        }).ToList();
    }

    public List<ConfigMod> GetDependantMods(string guid)
    {
        var listOfDependantMods = new List<ConfigMod>();

        var mods = GetMods();
        foreach (var (_, mod) in mods)
        {
            if (mod.Dependencies.ContainsKey(guid))
            {
                listOfDependantMods.Add(mod);
            }
        }

        return listOfDependantMods;
    }
}
