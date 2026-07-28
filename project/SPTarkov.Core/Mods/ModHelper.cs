using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.Forge;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.Mods;

public class ModHelper
{
    private readonly ILogger<ModHelper> _logger;
    private readonly HttpClient _httpClient;
    private readonly ConfigHelper _configHelper;
    private readonly ConcurrentDictionary<string, IModTask> _modDict = new(StringComparer.OrdinalIgnoreCase);
    private readonly SevenZip.SevenZip _sevenZip;

    public bool HasActiveTasks
    {
        get { return _modDict.Values.Any(task => task.Error is null && !task.Complete); }
    }

    public ModHelper(ILogger<ModHelper> logger, ConfigHelper configHelper, SevenZip.SevenZip sevenZip)
    {
        _logger = logger;
        _configHelper = configHelper;
        _sevenZip = sevenZip;

        // leaving default atm, this will be making requests to unknown servers.
        var handler = new HttpClientHandler { UseCookies = false };

        _httpClient = new HttpClient(handler);
    }

    public async Task<DownloadTask?> StartDownloadTask(
        ForgeBase mod,
        ForgeModVersion version,
        CancellationTokenSource cancellationTokenSource
    )
    {
        if (mod.GUID is null)
        {
            _logger.LogError("Mod {name} has no GUID, cannot start download task", mod.Name);
            return null;
        }

        var downloadTask = new DownloadTask
        {
            ForgeMod = mod,
            Version = version,
            TotalToDownload = 0,
            Progress = 0,
            CancellationTokenSource = cancellationTokenSource,
            Complete = false,
            Error = null,
        };

        if (!TryAddModTask(mod.GUID, mod.Name, downloadTask))
        {
            return null;
        }

        var modFilePath = Path.Join(Paths.ModCache, mod.GUID);
        try
        {
            if (!Directory.Exists(Paths.ModCache))
            {
                Directory.CreateDirectory(Paths.ModCache);
            }

            if (File.Exists(modFilePath))
            {
                File.Delete(modFilePath);
            }

            // Use a download to EFT client to test a long download
            using var response = await _httpClient.GetAsync(
                version.Link,
                HttpCompletionOption.ResponseHeadersRead,
                downloadTask.CancellationTokenSource.Token
            );
            response.EnsureSuccessStatusCode();

            downloadTask.TotalToDownload = response.Content.Headers.ContentLength ?? -1;

            await using var contentStream = await response.Content.ReadAsStreamAsync(downloadTask.CancellationTokenSource.Token);
            await using var fileStream = File.Create(modFilePath);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            var lastReportTime = DateTime.UtcNow;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, downloadTask.CancellationTokenSource.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, downloadTask.CancellationTokenSource.Token);
                totalRead += bytesRead;

                var now = DateTime.UtcNow;

                if (downloadTask.TotalToDownload > 0 && ((now - lastReportTime).TotalSeconds >= 1 || totalRead == downloadTask.TotalToDownload))
                {
                    downloadTask.Progress = (float)totalRead / downloadTask.TotalToDownload * 100;
                    lastReportTime = now;
                }
            }
        }
        catch (Exception e)
        {
            downloadTask.Error = e;
            await downloadTask.CancellationTokenSource.CancelAsync();
            TryDeleteFile(modFilePath);
            return downloadTask;
        }

        downloadTask.Progress = 100;
        downloadTask.Complete = true;
        return downloadTask;
    }

    // Registers a task for the mod, replacing any finished task. Fails while a task is still active.
    private bool TryAddModTask(string guid, string name, IModTask task)
    {
        while (!_modDict.TryAdd(guid, task))
        {
            if (_modDict.TryGetValue(guid, out var existing) && existing.Error is null && !existing.Complete)
            {
                _logger.LogWarning("A task is already running for {name}:{guid}", name, guid);
                return false;
            }

            _modDict.TryRemove(guid, out _);
        }

        return true;
    }

    public void RemoveModTask(IModTask task)
    {
        var guid = "";
        var name = "";

        switch (task)
        {
            case DownloadTask downloadTask:
                guid = downloadTask.ForgeMod.GUID ?? "";
                name = downloadTask.ForgeMod.Name;
                break;
            case UpdateTask updateTask:
                guid = updateTask.GUID;
                name = updateTask.Name;
                break;
            case InstallTask installTask:
                guid = installTask.ForgeMod.GUID;
                name = installTask.ForgeMod.Name;
                break;
        }

        if (!_modDict.TryRemove(guid, out _))
        {
            _logger.LogError("Unable to remove mod from download Dictionary for {name}:{guid}", name, guid);
        }
    }

    public async Task<bool> CancelModTask(string guid)
    {
        try
        {
            if (!_modDict.TryRemove(guid, out IModTask? downloadTask))
            {
                _logger.LogError("Couldn't remove download task for {guid}", guid);
                return false;
            }

            await downloadTask.CancellationTokenSource.CancelAsync();

            if (downloadTask is not DownloadTask)
            {
                _logger.LogInformation("Mod {guid} cancelled", guid);

                return true;
            }

            if (File.Exists(Path.Join(Paths.ModCache, guid)))
            {
                File.Delete(Path.Join(Paths.ModCache, guid));
            }

            _logger.LogInformation("ModDownload {guid} cancelled", guid);

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Couldn't cancel mod {guid} - {e}", guid, e.Message);
            return false;
        }
    }

    public ConcurrentDictionary<string, IModTask> GetModTasks()
    {
        return _modDict;
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning("Unable to delete partial download {path}: {message}", path, e.Message);
        }
    }

    // Downloads an archive straight to the given path, replacing any existing file.
    public async Task<bool> TryRedownloadArchive(string link, string filePath, CancellationToken token)
    {
        try
        {
            TryDeleteFile(filePath);

            using var response = await _httpClient.GetAsync(link, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();

            await using var contentStream = await response.Content.ReadAsStreamAsync(token);
            await using var fileStream = File.Create(filePath);
            await contentStream.CopyToAsync(fileStream, token);

            return true;
        }
        catch (Exception e)
        {
            _logger.LogWarning("Re-download failed for {link}: {message}", link, e.Message);
            TryDeleteFile(filePath);
            return false;
        }
    }

    public async Task<UpdateTask?> StartUpdateTask(ForgeModUpdate mod, CancellationTokenSource cancellationTokenSource)
    {
        var updateTask = new UpdateTask
        {
            Name = mod.CurrentVersion.Name!,
            Version = mod.RecommendedVersion.Version!,
            GUID = mod.CurrentVersion.GUID,
            Link = mod.RecommendedVersion.Link!,
            Progress = 0,
            TotalToDownload = 0,
            CancellationTokenSource = cancellationTokenSource,
            Complete = false,
            Error = null,
        };

        if (!TryAddModTask(updateTask.GUID, updateTask.Name, updateTask))
        {
            return null;
        }

        var modFilePath = Path.Join(Paths.ModCache, updateTask.GUID);

        try
        {
            if (!Directory.Exists(Paths.ModCache))
            {
                Directory.CreateDirectory(Paths.ModCache);
            }

            if (File.Exists(modFilePath))
            {
                File.Delete(modFilePath);
            }

            // Use a download to EFT client to test a long download
            using var response = await _httpClient.GetAsync(
                updateTask.Link,
                HttpCompletionOption.ResponseHeadersRead,
                updateTask.CancellationTokenSource.Token
            );
            response.EnsureSuccessStatusCode();

            updateTask.TotalToDownload = response.Content.Headers.ContentLength ?? -1;

            await using var contentStream = await response.Content.ReadAsStreamAsync(updateTask.CancellationTokenSource.Token);
            await using var fileStream = File.Create(modFilePath);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            var lastReportTime = DateTime.UtcNow;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, updateTask.CancellationTokenSource.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, updateTask.CancellationTokenSource.Token);
                totalRead += bytesRead;

                var now = DateTime.UtcNow;

                if (updateTask.TotalToDownload > 0 && ((now - lastReportTime).TotalSeconds >= 1 || totalRead == updateTask.TotalToDownload))
                {
                    updateTask.Progress = (float)totalRead / updateTask.TotalToDownload * 100;
                    lastReportTime = now;
                }
            }
        }
        catch (Exception e)
        {
            updateTask.Error = e;
            await updateTask.CancellationTokenSource.CancelAsync();
            TryDeleteFile(modFilePath);
            return updateTask;
        }

        updateTask.Progress = 100;
        updateTask.Complete = true;
        return updateTask;
    }

    public async Task<InstallTask?> StartInstallTask(ConfigMod mod, CancellationTokenSource cancellationTokenSource)
    {
        var installTask = new InstallTask
        {
            ForgeMod = mod,
            CancellationTokenSource = cancellationTokenSource,
            TotalToDownload = 0,
            Progress = 0,
            Complete = false,
            Error = null,
        };

        if (!TryAddModTask(installTask.ForgeMod.GUID, installTask.ForgeMod.Name, installTask))
        {
            return null;
        }

        var modFilePath = Path.Join(Paths.ModCache, mod.GUID);

        try
        {
            var entries = await _sevenZip.GetEntriesAsync(modFilePath, installTask.CancellationTokenSource.Token);

            // check if zip contains bepinex or spt folder for correct starting structure
            // this should be bepinex\ on windows and bepinex/ on linux
            var checkForCorrectFilePath = entries.Any(x =>
                x.ToLower().Contains("bepinex" + Path.DirectorySeparatorChar)
                || x.ToLower().Contains("spt_runtime" + Path.DirectorySeparatorChar)
            );

            // we checked this before, but to be sure
            if (!checkForCorrectFilePath)
            {
                _logger.LogError("Zip does not contain a bepinex or spt folder, unsupported structure, please report to SPT staff");
                installTask.Complete = false;
                installTask.Error = new Exception(
                    "Zip does not contain a bepinex or spt folder, unsupported structure, please report to SPT staff"
                );
                return installTask;
            }

            var extracted = await _sevenZip.ExtractToDirectoryAsync(
                modFilePath,
                _configHelper.GetConfig().GamePath,
                installTask.CancellationTokenSource.Token
            );

            if (!extracted)
            {
                _logger.LogError("Extraction failed for mod {name}:{guid}", mod.Name, mod.GUID);
                installTask.Error = new Exception($"Extraction failed for mod {mod.Name}");
                return installTask;
            }
        }
        catch (Exception e)
        {
            installTask.Error = e;
            await installTask.CancellationTokenSource.CancelAsync();
            return installTask;
        }

        installTask.Complete = true;

        return installTask;
    }
}
