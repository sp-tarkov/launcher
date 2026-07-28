using Microsoft.Extensions.Logging;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.Forge;
using SPTarkov.Core.Forge.Responses;
using SPTarkov.Core.Helpers;
using SPTarkov.Core.SPT;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.Mods;

public class ModManager(
    ILogger<ModManager> logger,
    ConfigHelper configHelper,
    ModHelper modHelper,
    ModTrackingStore modStore,
    SevenZip.SevenZip sevenZip,
    HttpHelper httpHelper
)
{
    private static readonly StringComparer _pathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    public event Action<ModNotification>? OnModNotification;

    private void Notify(ModOperation operation, string guid, string name)
    {
        OnModNotification?.Invoke(new ModNotification(operation, guid, name));
    }

    public async Task DownloadMod(
        ForgeBase forgeMod,
        ForgeModVersion version,
        CancellationToken cancellationToken = default,
        Dictionary<string, Version>? dictOfDeps = null,
        bool notify = true
    )
    {
        dictOfDeps ??= new Dictionary<string, Version>();

        ConfigMod? existing = null;
        if (forgeMod.GUID is not null)
        {
            GetMods().TryGetValue(forgeMod.GUID, out existing);
        }

        // Downloading a different version of an installed mod is an update
        if (existing is { IsInstalled: true } && !Equals(existing.ModVersion, version.Version))
        {
            await UpdateMod(BuildVersionChangeUpdate(existing, forgeMod, version), cancellationToken);
            return;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // start the download
        var downloadTask = await modHelper.StartDownloadTask(forgeMod, version, cts);

        if (downloadTask == null)
        {
            logger.LogError("Download task failed for mod {mod}", forgeMod.Name);
            return;
        }

        if (!downloadTask.Complete)
        {
            logger.LogError("Download task failed for mod {mod}: {e}", forgeMod.Name, downloadTask.Error);
            return;
        }

        try
        {
            var configMod = await ConvertToConfigMod(downloadTask);

            if (configMod == null)
            {
                logger.LogError("configMod is null, download error: {downloadTask}", downloadTask.Error);
                return;
            }

            modHelper.RemoveModTask(downloadTask);

            configMod.Dependencies = dictOfDeps.Count > 0 ? dictOfDeps : existing?.Dependencies ?? dictOfDeps;
            configMod.IsInstalled = existing is { IsInstalled: true };
            modStore.AddMod(configMod);

            if (notify && !configMod.IsInstalled)
            {
                Notify(ModOperation.Downloaded, configMod.GUID, configMod.Name);
            }
        }
        catch (Exception e) // callers fire-and-forget this task, errors must catch here for display/logging
        {
            downloadTask.Error = e;
            logger.LogError("Download task failed for mod {mod}: {e}", forgeMod.Name, e);
            return;
        }

        logger.LogDebug("Download task completed");
    }

    private async Task<ConfigMod?> ConvertToConfigMod(DownloadTask downloadTask)
    {
        var modGuid = downloadTask.ForgeMod.GUID;
        if (modGuid is null)
        {
            downloadTask.Error = new Exception("Mod has no GUID");
            await downloadTask.CancellationTokenSource.CancelAsync();
            return null;
        }

        var modFilePath = Path.Join(Paths.ModCache, modGuid);
        if (!File.Exists(modFilePath))
        {
            downloadTask.Error = new FileNotFoundException("file not found", modFilePath);
            await downloadTask.CancellationTokenSource.CancelAsync();
            return null;
        }

        var entries = await GetArchiveEntries(
            modFilePath,
            downloadTask.ForgeMod.Id,
            downloadTask.Version.Id,
            downloadTask.Version.Link,
            downloadTask.CancellationTokenSource.Token
        );

        if (entries == null)
        {
            downloadTask.Error = new Exception("Unable to read the archive contents.");
            await downloadTask.CancellationTokenSource.CancelAsync();
            return null;
        }

        // Check if archive contains BepInEx or SPT_Runtime folder
        var checkForCorrectFilePath = entries.Any(x =>
            x.ToLower().Contains("bepinex" + Path.DirectorySeparatorChar)
            || x.ToLower().Contains("spt_runtime" + Path.DirectorySeparatorChar)
        );

        if (checkForCorrectFilePath)
        {
            return new ConfigMod
            {
                Name = downloadTask.ForgeMod.Name,
                ModVersion = downloadTask.Version.Version,
                GUID = modGuid,
                ModId = downloadTask.ForgeMod.Id,
                VersionId = downloadTask.Version.Id,
                IsInstalled = false,
                CanBeUpdated = false,
                Files = RemoveBasePaths(entries),
            };
        }

        downloadTask.Error = new Exception("Archive does not contain a BepInEx or SPT_Runtime folder. Unsupported structure.");
        await downloadTask.CancellationTokenSource.CancelAsync();
        return null;
    }

    // Resolves the full dependency tree for a mod version through the Forge. Null when the request fails.
    public async Task<DependencyResolution?> ResolveDependencies(
        string identifier,
        Version version,
        CancellationToken cancellationToken = default
    )
    {
        ForgeDependencyResponse? response;
        try
        {
            response = await httpHelper.ForgeGetModDependencies([$"{identifier}:{version}"], cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogWarning("Dependency resolution request failed for {identifier}: {message}", identifier, e.Message);
            return null;
        }

        if (response is not { Success: true, Data: not null })
        {
            return null;
        }

        var resolution = new DependencyResolution { DirectDependencies = response.Data };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<ForgeDependencyNode>(response.Data);

        while (queue.TryDequeue(out var node))
        {
            if (node.GUID is null)
            {
                resolution.Unresolvable.Add(node);
                continue;
            }

            var firstVisit = seen.Add(node.GUID);

            if (node.Conflict)
            {
                resolution.Conflicted.Add(node);
            }
            else if (firstVisit && node.LatestCompatibleVersion is { Link: not null, Version: not null })
            {
                resolution.Resolved.Add(node);
            }
            else if (firstVisit)
            {
                resolution.Unresolvable.Add(node);
            }

            if (firstVisit)
            {
                foreach (var child in node.Dependencies ?? [])
                {
                    queue.Enqueue(child);
                }
            }
        }

        return resolution;
    }

    // Downloads every resolved dependency and then the mod itself.
    public async Task DownloadModWithDependencies(
        ForgeBase forgeMod,
        ForgeModVersion version,
        DependencyResolution resolution,
        CancellationToken cancellationToken = default
    )
    {
        await DownloadResolvedDependencies(resolution, cancellationToken);
        await DownloadMod(forgeMod, version, cancellationToken, BuildDependencyDict(resolution.DirectDependencies));
    }

    // Downloads each resolved dependency that is not already tracked at its resolved version.
    private async Task DownloadResolvedDependencies(DependencyResolution resolution, CancellationToken cancellationToken)
    {
        foreach (var dep in resolution.Resolved)
        {
            var depVersion = dep.LatestCompatibleVersion!;
            if (GetMods().TryGetValue(dep.GUID!, out var tracked) && Equals(tracked.ModVersion, depVersion.Version))
            {
                continue;
            }

            var forgeBase = new ForgeBase
            {
                Id = dep.Id,
                GUID = dep.GUID,
                Name = dep.Name,
                Slug = dep.Slug,
            };
            var forgeVersion = new ForgeModVersion
            {
                Id = depVersion.Id,
                Link = depVersion.Link!,
                Version = depVersion.Version!,
                ContentLength = depVersion.ContentLength,
            };

            await DownloadMod(forgeBase, forgeVersion, cancellationToken, BuildDependencyDict(dep.Dependencies), notify: false);
        }
    }

    // Maps immediate dependency nodes to their resolved versions.
    public static Dictionary<string, Version> BuildDependencyDict(List<ForgeDependencyNode>? nodes)
    {
        var dependencies = new Dictionary<string, Version>();
        foreach (var node in nodes ?? [])
        {
            if (node.GUID is not null && node.LatestCompatibleVersion?.Version is { } nodeVersion)
            {
                dependencies.TryAdd(node.GUID, nodeVersion);
            }
        }

        return dependencies;
    }

    // Builds a message listing the conflicted and unresolvable dependencies.
    public static string DescribeBlockedDependencies(DependencyResolution resolution)
    {
        var parts = new List<string>();

        if (resolution.Conflicted.Count > 0)
        {
            var conflicts = resolution
                .Conflicted.GroupBy(x => x.GUID ?? x.Name)
                .Select(g =>
                    $"{g.First().Name} ({string.Join(", ", g.Select(x => x.LatestCompatibleVersion?.Version?.ToString() ?? "?").Distinct())})"
                );
            parts.Add($"conflicting dependency versions: {string.Join("; ", conflicts)}");
        }

        if (resolution.Unresolvable.Count > 0)
        {
            parts.Add($"no compatible dependency version: {string.Join("; ", resolution.Unresolvable.Select(x => x.Name).Distinct())}");
        }

        return string.Join(" | ", parts);
    }

    public Dictionary<string, ConfigMod> GetMods()
    {
        return modStore.GetMods();
    }

    public async Task<bool> InstallMod(string guid, CancellationToken cancellationToken = default, bool notify = true)
    {
        var modFilePath = Path.Join(Paths.ModCache, guid);
        if (!File.Exists(modFilePath))
        {
            logger.LogError("file not found: {file}", modFilePath);
            return false;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var configMod = GetMods().FirstOrDefault(x => x.Key == guid).Value;
        logger.LogInformation("Installing mod: {guid}", guid);

        try
        {
            var installTask = await modHelper.StartInstallTask(configMod, cts);

            if (installTask is not { Complete: true } || installTask.Error != null)
            {
                // TODO: something fucked up, do something or cancelled
                logger.LogError("install task failed for mod {mod}: {e}", guid, installTask?.Error);
                return false;
            }

            modHelper.RemoveModTask(installTask);
        }
        catch (Exception e)
        {
            logger.LogWarning("install task failed for reason:  {reason}", e.Message);
            return false;
        }

        logger.LogInformation("Installed mod: {guid}", guid);
        configMod.IsInstalled = true;
        modStore.AddMod(configMod);

        if (notify)
        {
            Notify(ModOperation.Installed, configMod.GUID, configMod.Name);
        }

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
                await InstallMod(depGuid, notify: false);
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
                    await UninstallMod(depGuid, notify: false);
                }
            }
        }
    }

    public async Task<bool> UninstallMod(string guid, bool notify = true)
    {
        if (!GetMods().ContainsKey(guid))
        {
            logger.LogError("key not found: {key}", guid);
            return false;
        }

        if (!GetMods().TryGetValue(guid, out var mod))
        {
            logger.LogError("unable to get key: {key}", guid);
            return false;
        }

        // Check if there are any mods that depend on this one, if so, do not uninstall it
        var checkForDependOnThis = GetMods()
            .Any(x => x.Value.Dependencies != null && x.Value.Dependencies.ContainsKey(guid) && x.Value.IsInstalled);

        if (checkForDependOnThis)
        {
            // DONT REMOVE MOD, SOMETHING DEPENDS ON IT
            // TODO: show feedback to user that this cant be uninstalled
            return false;
        }

        if (!RemoveInstalledFiles(mod))
        {
            logger.LogWarning("Some files could not be removed for mod {guid}, uninstall can be retried", guid);
            return false;
        }

        logger.LogInformation("uninstalled mod: {guid}", guid);

        var configMod = GetMods().FirstOrDefault(x => x.Key == guid).Value;
        configMod.IsInstalled = false;

        modStore.AddMod(configMod);

        if (notify)
        {
            Notify(ModOperation.Uninstalled, configMod.GUID, configMod.Name);
        }

        await UninstallModDependencies(guid);

        return true;
    }

    public void DeleteMod(string guid)
    {
        if (!GetMods().ContainsKey(guid))
        {
            logger.LogError("key not found: {key}", guid);
            return;
        }

        if (!GetMods().TryGetValue(guid, out var mod))
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

        if (mod.IsInstalled && !RemoveInstalledFiles(mod))
        {
            logger.LogWarning("Some files could not be removed for mod {guid}, delete can be retried", guid);
            return;
        }

        logger.LogInformation("Deleted mod: {guid}", guid);

        try
        {
            if (File.Exists(Path.Join(Paths.ModCache, guid)))
            {
                logger.LogInformation("deleted zip for mod {guid}", guid);
                File.Delete(Path.Join(Paths.ModCache, guid));
            }
        }
        catch (Exception e)
        {
            logger.LogWarning("Unable to delete zip for mod {guid}: {message}", guid, e.Message);
        }

        modStore.RemoveMod(guid);
        Notify(ModOperation.Deleted, mod.GUID, mod.Name);
    }

    public async Task UpdateMod(ForgeModUpdate mod, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (!GetMods().TryGetValue(mod.CurrentVersion.GUID, out var configMod))
        {
            logger.LogError("unable to get key: {key}", mod.CurrentVersion.GUID);
            return;
        }

        var wasInstalled = configMod.IsInstalled;
        var ogPath = Path.Join(Paths.ModCache, mod.CurrentVersion.GUID);
        var bakPath = ogPath + ".bak";

        // copy current version to be .bak
        try
        {
            if (File.Exists(ogPath))
            {
                File.Copy(ogPath, bakPath, true);
            }
        }
        catch (Exception e)
        {
            logger.LogError("unable to back up {file}: {message}", ogPath, e.Message);
            return;
        }

        var updateTask = await modHelper.StartUpdateTask(mod, cts);

        if (updateTask is not { Complete: true } || updateTask.Error != null)
        {
            logger.LogError("Update task failed for mod {mod}: {e}", mod.CurrentVersion.Name, updateTask?.Error);
            RestoreCacheBackup(bakPath, ogPath);
            return;
        }

        DependencyResolution? resolution;

        try
        {
            var entries = await GetArchiveEntries(
                ogPath,
                mod.RecommendedVersion.ModId,
                mod.RecommendedVersion.Id,
                mod.RecommendedVersion.Link,
                cts.Token
            );

            if (entries == null)
            {
                updateTask.Error = new Exception("Unable to read the archive contents.");
                RestoreCacheBackup(bakPath, ogPath);
                return;
            }

            // Check if archive contains BepInEx or SPT_Runtime folder
            var checkForCorrectFilePath = entries.Any(x =>
                x.ToLower().Contains("bepinex" + Path.DirectorySeparatorChar)
                || x.ToLower().Contains("spt_runtime" + Path.DirectorySeparatorChar)
            );

            if (!checkForCorrectFilePath)
            {
                updateTask.Error = new Exception("Archive does not contain a BepInEx or SPT_Runtime folder. Unsupported structure.");
                RestoreCacheBackup(bakPath, ogPath);
                return;
            }

            resolution = mod.RecommendedVersion.Version is { } newVersion
                ? await ResolveDependencies(configMod.GUID, newVersion, cts.Token)
                : null;

            if (resolution is null)
            {
                updateTask.Error = new Exception("Unable to resolve the mod's dependencies.");
                RestoreCacheBackup(bakPath, ogPath);
                return;
            }

            if (resolution.IsBlocked)
            {
                updateTask.Error = new Exception(DescribeBlockedDependencies(resolution));
                RestoreCacheBackup(bakPath, ogPath);
                return;
            }

            // Remove the previous version's files while the manifest still describes them
            if (wasInstalled && !RemoveInstalledFiles(configMod))
            {
                updateTask.Error = new Exception("Unable to remove the previous version's files.");
                RestoreCacheBackup(bakPath, ogPath);
                return;
            }

            // Update config for latest version
            configMod.IsInstalled = false;
            configMod.ModVersion = mod.RecommendedVersion.Version;
            configMod.ModId = mod.RecommendedVersion.ModId ?? configMod.ModId;
            configMod.VersionId = mod.RecommendedVersion.Id ?? configMod.VersionId;
            configMod.Files = RemoveBasePaths(entries);
            configMod.Dependencies = BuildDependencyDict(resolution.DirectDependencies);
            modStore.AddMod(configMod);
        }
        catch (Exception e)
        {
            logger.LogError("Update failed for mod {mod}: {e}", mod.CurrentVersion.Name, e);
            updateTask.Error = e;
            RestoreCacheBackup(bakPath, ogPath);
            return;
        }

        // delete old zip with .bak
        try
        {
            File.Delete(bakPath);
        }
        catch (Exception e)
        {
            logger.LogWarning("Unable to delete backup {file}: {message}", bakPath, e.Message);
        }

        modHelper.RemoveModTask(updateTask);

        await DownloadResolvedDependencies(resolution, cancellationToken);

        // Install the new version when the previous one was installed
        if (wasInstalled && !await InstallMod(configMod.GUID, cancellationToken, notify: false))
        {
            logger.LogError("Unable to install the updated version of mod {guid}", configMod.GUID);
            return;
        }

        Notify(ModOperation.Updated, configMod.GUID, configMod.Name);
    }

    // Builds an update request that moves an installed mod to the given version.
    private static ForgeModUpdate BuildVersionChangeUpdate(ConfigMod existing, ForgeBase forgeMod, ForgeModVersion version)
    {
        return new ForgeModUpdate
        {
            CurrentVersion = new UpdateMod
            {
                GUID = existing.GUID,
                Name = existing.Name,
                Version = existing.ModVersion,
            },
            RecommendedVersion = new UpdateMod
            {
                Id = version.Id,
                ModId = forgeMod.Id,
                GUID = existing.GUID,
                Name = forgeMod.Name,
                Version = version.Version,
                Link = version.Link,
            },
            UpdateReason = "version change",
        };
    }

    // Moves the cache backup back over the original zip.
    private void RestoreCacheBackup(string bakPath, string ogPath)
    {
        try
        {
            if (File.Exists(bakPath))
            {
                File.Move(bakPath, ogPath, true);
            }
        }
        catch (Exception e)
        {
            logger.LogWarning("Unable to restore backup {file}: {message}", bakPath, e.Message);
        }
    }

    // Gets the archive file listing: the local 7-Zip listing first, the Forge file tree second, a fresh re-download last.
    private async Task<List<string>?> GetArchiveEntries(
        string archivePath,
        int? modId,
        int? versionId,
        string? link,
        CancellationToken token
    )
    {
        try
        {
            return await sevenZip.GetEntriesAsync(archivePath, token);
        }
        catch (Exception e)
        {
            logger.LogWarning("Unable to list archive {archive}: {message}", archivePath, e.Message);
        }

        if (modId is not null && versionId is not null)
        {
            try
            {
                var fileTree = await httpHelper.ForgeGetModVersionFileTree(modId.Value, versionId.Value, token);
                if (fileTree?.Data?.Files is { Count: > 0 } files)
                {
                    if (fileTree.Data.Truncated)
                    {
                        logger.LogWarning(
                            "File tree for mod {modId} version {versionId} is truncated, the manifest may be incomplete",
                            modId,
                            versionId
                        );
                    }

                    return files.Select(x => x.Replace('/', Path.DirectorySeparatorChar)).ToList();
                }
            }
            catch (Exception e)
            {
                logger.LogWarning("File tree request failed for mod {modId} version {versionId}: {message}", modId, versionId, e.Message);
            }
        }

        if (!string.IsNullOrEmpty(link) && await modHelper.TryRedownloadArchive(link, archivePath, token))
        {
            try
            {
                return await sevenZip.GetEntriesAsync(archivePath, token);
            }
            catch (Exception e)
            {
                logger.LogWarning("Unable to list re-downloaded archive {archive}: {message}", archivePath, e.Message);
            }
        }

        return null;
    }

    // Deletes the manifest files, then prunes manifest directories left empty.
    private bool RemoveInstalledFiles(ConfigMod mod)
    {
        if (mod.Files == null)
        {
            return true;
        }

        var directories = new List<string>();
        var failures = 0;

        foreach (var file in mod.Files)
        {
            var fullPath = ResolveInstalledFilePath(file);
            if (fullPath is null)
            {
                continue;
            }

            if (Directory.Exists(fullPath))
            {
                directories.Add(fullPath);
                continue;
            }

            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (Exception e)
            {
                logger.LogWarning("Unable to delete mod file {file}: {message}", fullPath, e.Message);
                failures++;
            }
        }

        foreach (var directory in directories.OrderByDescending(x => x.Length))
        {
            try
            {
                if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            catch (Exception e)
            {
                logger.LogWarning("Unable to delete mod directory {directory}: {message}", directory, e.Message);
                failures++;
            }
        }

        PruneEmptyParentDirectories(mod.Files);

        return failures == 0;
    }

    // Removes directories the mod's files left empty, walking each parent chain up to the protected base directories.
    private void PruneEmptyParentDirectories(List<string> files)
    {
        var gameRoot = Path.GetFullPath(configHelper.GetConfig().GamePath);
        var protectedRoots = new HashSet<string>(_pathComparer) { gameRoot };
        foreach (var basePath in Paths.ArchiveFileInfoToIgnore)
        {
            protectedRoots.Add(Path.GetFullPath(Path.Join(gameRoot, basePath)));
        }

        var candidates = new HashSet<string>(_pathComparer);

        foreach (var file in files)
        {
            var fullPath = ResolveInstalledFilePath(file);
            var parent = fullPath is null ? null : Path.GetDirectoryName(fullPath);

            while (parent != null && parent.Length > gameRoot.Length && !protectedRoots.Contains(parent))
            {
                candidates.Add(parent);
                parent = Path.GetDirectoryName(parent);
            }
        }

        foreach (var directory in candidates.OrderByDescending(x => x.Length))
        {
            try
            {
                if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            catch (Exception e)
            {
                logger.LogDebug("Unable to prune directory {directory}: {message}", directory, e.Message);
            }
        }
    }

    // Resolves a stored mod file path against the game directory.
    private string? ResolveInstalledFilePath(string file)
    {
        var gameRoot = Path.GetFullPath(configHelper.GetConfig().GamePath);
        var gameRootWithSeparator = gameRoot.EndsWith(Path.DirectorySeparatorChar) ? gameRoot : gameRoot + Path.DirectorySeparatorChar;

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(Path.Join(gameRoot, file));
        }
        catch (Exception e)
        {
            logger.LogWarning("Skipping mod file with an invalid path {file}: {message}", file, e.Message);
            return null;
        }

        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (fullPath.StartsWith(gameRootWithSeparator, comparison))
        {
            return fullPath;
        }

        logger.LogWarning("Skipping mod file outside the game directory: {file}", file);
        return null;
    }

    private static List<string> RemoveBasePaths(List<string> originalPaths)
    {
        return originalPaths.Where(x => !Paths.ArchiveFileInfoToIgnore.Contains(x)).ToList();
    }

    public List<ConfigMod> GetDependantMods(string guid)
    {
        var listOfDependantMods = new List<ConfigMod>();

        var mods = GetMods();
        foreach (var (_, mod) in mods)
        {
            if (mod.Dependencies != null && mod.Dependencies.ContainsKey(guid))
            {
                listOfDependantMods.Add(mod);
            }
        }

        return listOfDependantMods;
    }
}
