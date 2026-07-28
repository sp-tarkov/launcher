using Microsoft.Extensions.Logging;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.Forge;
using SPTarkov.Core.Helpers;
using SPTarkov.Core.Mods.Inspection;
using SPTarkov.Core.SPT;
using Version = SemanticVersioning.Version;

namespace SPTarkov.Core.Mods;

/// <summary>
/// Rebuilds mod tracking state from the install directory. Mod DLLs are statically inspected for their GUID, name, and
/// version, then reconciled against the tracking store so manual installs, updates, and removals are picked up. Only
/// mods the forge knows are tracked; every other discovered component is ignored.
/// </summary>
public class ModReconciler(
    ILogger<ModReconciler> logger,
    ConfigHelper configHelper,
    ModTrackingStore modStore,
    ModHelper modHelper,
    ModManager modManager,
    HttpHelper httpHelper,
    SevenZip.SevenZip sevenZip
)
{
    private const int GuidLookupChunkSize = 10;

    private int _running;

    /// <summary>Scans the install directory and reconciles what it finds with the tracking store.</summary>
    public async Task ReconcileAsync(CancellationToken token = default)
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            return;
        }

        try
        {
            if (modHelper.HasActiveTasks)
            {
                logger.LogDebug("Skipping mod reconcile while mod tasks are active");
                return;
            }

            var discovered = ScanInstallDirectory();
            await ReconcileDiscovered(discovered, token);
            ReconcileRemoved(discovered);
        }
        catch (Exception e)
        {
            logger.LogError("Mod reconcile failed: {e}", e);
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }

    /// <summary>Finds mod components in the install directory's DLLs, grouped by mod GUID.</summary>
    private Dictionary<string, DiscoveredMod> ScanInstallDirectory()
    {
        var gamePath = Path.GetFullPath(configHelper.GetConfig().GamePath);
        var excludedRoot = Path.Join(gamePath, "BepInEx", "plugins", "spt") + Path.DirectorySeparatorChar;
        var scanRoots = new[]
        {
            Path.Join(gamePath, "BepInEx", "plugins"),
            Path.Join(gamePath, "BepInEx", "patchers"),
            Path.Join(gamePath, "SPT_Runtime", "user", "mods"),
        };

        var discovered = new Dictionary<string, DiscoveredMod>(StringComparer.OrdinalIgnoreCase);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (var root in scanRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var dllPath in Directory.EnumerateFiles(root, "*.dll", SearchOption.AllDirectories))
            {
                if (dllPath.StartsWith(excludedRoot, comparison))
                {
                    continue;
                }

                var relativePath = Path.GetRelativePath(gamePath, dllPath);

                foreach (var finding in DllMetadataReader.Read(dllPath, relativePath))
                {
                    if (finding.Guid is null)
                    {
                        continue;
                    }

                    if (!discovered.TryGetValue(finding.Guid, out var mod))
                    {
                        mod = new DiscoveredMod(finding.Guid);
                        discovered[finding.Guid] = mod;
                    }

                    mod.Name ??= finding.Name;

                    if (!mod.Files.Contains(relativePath))
                    {
                        mod.Files.Add(relativePath);
                    }

                    var version = ParseVersion(finding.Version);
                    if (version != null && (mod.Version == null || version > mod.Version))
                    {
                        mod.Version = version;
                    }
                }
            }
        }

        logger.LogInformation("Install scan found {count} mods", discovered.Count);
        return discovered;
    }

    /// <summary>Tracks discovered mods that are missing from the store and refreshes ones whose on-disk version changed.</summary>
    private async Task ReconcileDiscovered(Dictionary<string, DiscoveredMod> discovered, CancellationToken token)
    {
        var mods = modStore.GetMods();
        var untracked = new List<DiscoveredMod>();
        var versionChanged = new List<(ConfigMod Tracked, DiscoveredMod Disk)>();

        foreach (var (guid, disk) in discovered)
        {
            if (!mods.TryGetValue(guid, out var tracked))
            {
                untracked.Add(disk);
                continue;
            }

            if (!tracked.IsInstalled)
            {
                logger.LogInformation("Mod {guid} is present on disk, marking as installed", guid);
                tracked.IsInstalled = true;
                modStore.AddMod(tracked);
            }

            if (disk.Version != null && tracked.ModVersion != null && disk.Version != tracked.ModVersion)
            {
                versionChanged.Add((tracked, disk));
            }
        }

        if (untracked.Count == 0 && versionChanged.Count == 0)
        {
            return;
        }

        var guidsToLookup = untracked.Select(x => x.Guid).Concat(versionChanged.Select(x => x.Tracked.GUID)).ToList();
        var forgeMods = await LookupForgeMods(guidsToLookup, token);

        foreach (var disk in untracked)
        {
            if (!forgeMods.TryGetValue(disk.Guid, out var forgeMod))
            {
                logger.LogDebug("Ignoring {guid}, not found on the forge", disk.Guid);
                continue;
            }

            await TrackDiscoveredMod(disk, forgeMod, token);
        }

        foreach (var (tracked, disk) in versionChanged)
        {
            await UpdateTrackedVersion(tracked, disk, forgeMods.GetValueOrDefault(tracked.GUID), token);
        }
    }

    /// <summary>Marks installed mods as uninstalled when no component and no manifest file remains on disk.</summary>
    private void ReconcileRemoved(Dictionary<string, DiscoveredMod> discovered)
    {
        foreach (var (guid, tracked) in modStore.GetMods().ToList())
        {
            if (!tracked.IsInstalled || discovered.ContainsKey(guid))
            {
                continue;
            }

            if (AnyManifestFileExists(tracked))
            {
                continue;
            }

            logger.LogInformation("Mod {guid} is no longer present on disk, marking as uninstalled", guid);
            tracked.IsInstalled = false;
            modStore.AddMod(tracked);
        }
    }

    /// <summary>Creates a tracking entry for a manually installed forge mod, recovering its manifest best-effort.</summary>
    private async Task TrackDiscoveredMod(DiscoveredMod disk, ForgeBase forgeMod, CancellationToken token)
    {
        var forgeVersion = await MatchForgeVersion(forgeMod, disk.Version, token);
        var manifest =
            await GetManifestFromCache(disk.Guid, token) ?? await GetManifestFromFileTree(forgeMod, forgeVersion, token) ?? disk.Files;
        var dependencies = await ResolveDependencyMap(disk.Guid, forgeVersion, token) ?? BuildDependencies(forgeVersion);

        var configMod = new ConfigMod
        {
            Name = forgeMod.Name,
            GUID = forgeMod.GUID ?? disk.Guid,
            ModVersion = disk.Version ?? forgeVersion?.Version ?? new Version(0, 0, 0),
            ModId = forgeMod.Id,
            VersionId = forgeVersion?.Id,
            IsInstalled = true,
            Files = manifest,
            Dependencies = dependencies,
        };

        logger.LogInformation("Tracking manually installed mod {name} ({guid})", configMod.Name, configMod.GUID);
        modStore.AddMod(configMod);
    }

    /// <summary>Updates a tracked mod to the version found on disk, refreshing its manifest when Forge knows the version.</summary>
    private async Task UpdateTrackedVersion(ConfigMod tracked, DiscoveredMod disk, ForgeBase? forgeMod, CancellationToken token)
    {
        logger.LogInformation("Mod {guid} changed on disk from {old} to {new}", tracked.GUID, tracked.ModVersion, disk.Version);

        var forgeVersion = await MatchForgeVersion(forgeMod, disk.Version, token);
        var manifest = await GetManifestFromFileTree(forgeMod, forgeVersion, token);
        var dependencies = await ResolveDependencyMap(tracked.GUID, forgeVersion, token);

        tracked.ModVersion = disk.Version;
        tracked.ModId = forgeMod?.Id ?? tracked.ModId;
        tracked.VersionId = forgeVersion?.Id ?? tracked.VersionId;

        if (manifest != null)
        {
            tracked.Files = manifest;
        }

        if (dependencies != null)
        {
            tracked.Dependencies = dependencies;
        }

        modStore.AddMod(tracked);
    }

    /// <summary>Resolves a mod version's immediate dependencies through the Forge. Null when resolution fails.</summary>
    private async Task<Dictionary<string, Version>?> ResolveDependencyMap(string guid, ForgeModVersion? forgeVersion, CancellationToken token)
    {
        if (forgeVersion?.Version is not { } version)
        {
            return null;
        }

        var resolution = await modManager.ResolveDependencies(guid, version, token, checkInstalledSet: false);
        return resolution is null ? null : ModManager.BuildDependencyDict(resolution.DirectDependencies);
    }

    /// <summary>Looks up Forge mods for the given GUIDs in chunks. Failures degrade to an empty result.</summary>
    private async Task<Dictionary<string, ForgeBase>> LookupForgeMods(List<string> guids, CancellationToken token)
    {
        var result = new Dictionary<string, ForgeBase>(StringComparer.OrdinalIgnoreCase);

        foreach (var chunk in guids.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(GuidLookupChunkSize))
        {
            try
            {
                var response = await httpHelper.ForgeGetModsByGuids(chunk.ToList(), token);
                foreach (var mod in response?.Data ?? [])
                {
                    if (mod.GUID != null)
                    {
                        result.TryAdd(mod.GUID, mod);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogWarning("Forge lookup failed for installed mods: {message}", e.Message);
            }
        }

        return result;
    }

    /// <summary>Finds the Forge version record matching the given version, checking embedded versions before querying.</summary>
    private async Task<ForgeModVersion?> MatchForgeVersion(ForgeBase? forgeMod, Version? version, CancellationToken token)
    {
        if (forgeMod == null || version == null)
        {
            return null;
        }

        var embedded = forgeMod.Versions?.FirstOrDefault(x => x.Version == version);
        if (embedded != null)
        {
            return embedded;
        }

        try
        {
            var response = await httpHelper.ForgeGetModVersionExact(forgeMod.Id.ToString(), version.ToString(), token);
            return response?.Data?.FirstOrDefault(x => x.Version == version);
        }
        catch (Exception e)
        {
            logger.LogWarning("Version lookup failed for {guid} {version}: {message}", forgeMod.GUID, version, e.Message);
            return null;
        }
    }

    /// <summary>Builds a manifest from the mod's cached archive, when one exists.</summary>
    private async Task<List<string>?> GetManifestFromCache(string guid, CancellationToken token)
    {
        var zipPath = Path.Join(Paths.ModCache, guid);
        if (!File.Exists(zipPath))
        {
            return null;
        }

        try
        {
            var entries = await sevenZip.GetEntriesAsync(zipPath, token);
            return entries.Where(x => !Paths.ArchiveFileInfoToIgnore.Contains(x)).ToList();
        }
        catch (Exception e)
        {
            logger.LogWarning("Unable to list cached archive for {guid}: {message}", guid, e.Message);
            return null;
        }
    }

    /// <summary>Builds a manifest from the Forge file tree for the given mod version, when available.</summary>
    private async Task<List<string>?> GetManifestFromFileTree(ForgeBase? forgeMod, ForgeModVersion? forgeVersion, CancellationToken token)
    {
        if (forgeMod == null || forgeVersion == null)
        {
            return null;
        }

        try
        {
            var fileTree = await httpHelper.ForgeGetModVersionFileTree(forgeMod.Id, forgeVersion.Id, token);
            if (fileTree?.Data?.Files is not { Count: > 0 } files)
            {
                return null;
            }

            if (fileTree.Data.Truncated)
            {
                logger.LogWarning("File tree for mod {guid} is truncated, the manifest may be incomplete", forgeMod.GUID);
            }

            return files.Select(x => x.Replace('/', Path.DirectorySeparatorChar)).ToList();
        }
        catch (Exception e)
        {
            logger.LogWarning("File tree request failed for {guid}: {message}", forgeMod.GUID, e.Message);
            return null;
        }
    }

    /// <summary>Builds the dependency map from a Forge version's dependency list.</summary>
    private static Dictionary<string, Version> BuildDependencies(ForgeModVersion? forgeVersion)
    {
        var dependencies = new Dictionary<string, Version>(StringComparer.OrdinalIgnoreCase);

        foreach (var dep in forgeVersion?.Dependencies ?? [])
        {
            var depVersion = dep.Versions?.FirstOrDefault()?.Version;
            if (dep.GUID != null && depVersion != null)
            {
                dependencies.TryAdd(dep.GUID, depVersion);
            }
        }

        return dependencies;
    }

    /// <summary>Determines whether any of the mod's manifest files still exist inside the game directory.</summary>
    private bool AnyManifestFileExists(ConfigMod mod)
    {
        if (mod.Files is not { Count: > 0 })
        {
            return true;
        }

        var gameRoot = Path.GetFullPath(configHelper.GetConfig().GamePath);
        var gameRootWithSeparator = gameRoot.EndsWith(Path.DirectorySeparatorChar) ? gameRoot : gameRoot + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (var file in mod.Files)
        {
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(Path.Join(gameRoot, file));
            }
            catch (Exception)
            {
                continue;
            }

            if (!fullPath.StartsWith(gameRootWithSeparator, comparison))
            {
                continue;
            }

            if (File.Exists(fullPath))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Parses a version string leniently, returning null when it cannot be parsed.</summary>
    private static Version? ParseVersion(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            return new Version(text, true);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>A mod discovered on disk: its GUID, best-known name and version, and the DLLs that declared it.</summary>
    private sealed record DiscoveredMod(string Guid)
    {
        public string? Name { get; set; }

        public Version? Version { get; set; }

        public List<string> Files { get; } = [];
    }
}
