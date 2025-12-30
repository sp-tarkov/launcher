using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.Forge;
using SPTarkov.Core.SPT;
using SPTarkov.Core.SevenZip;

namespace SPTarkov.Core.Mods;

public class ModHelper
{
    private ILogger<ModHelper> _logger;
    private HttpClient _httpClient;
    private ConfigHelper _configHelper;
    private ConcurrentDictionary<string, IModTask> _modDict = new();
    private SevenZip.SevenZip _sevenZip;

    public ModHelper(
        ILogger<ModHelper> logger,
        ConfigHelper configHelper,
        SevenZip.SevenZip sevenZip
    )
    {
        _logger = logger;
        _configHelper = configHelper;
        _sevenZip = sevenZip;

        // leaving default atm, this will be making requests to unknown servers.
        var handler = new HttpClientHandler();
        handler.UseCookies = false;
        _httpClient = new HttpClient(handler);
    }

    public async Task<DownloadTask?> StartDownloadTask(ForgeBase mod, ForgeModVersion version, CancellationTokenSource cancellationToken)
    {
        var downloadTask = new DownloadTask
        {
            ForgeMod = mod,
            Version = version,
            TotalToDownload = 0,
            Progress = 0,
            CancellationTokenSource = cancellationToken,
            Complete = false,
            Error = null
        };

        if (!_modDict.TryAdd(mod.Guid, downloadTask))
        {
            _modDict.Remove(mod.Guid, out _);
            if (!_modDict.TryAdd(mod.Guid, downloadTask))
            {
                _logger.LogError("Something seriously went wrong adding this download task: {name}:{guid}", mod.Name, mod.Guid);
                return null;
            }
        }

        var modFilePath = Path.Join(Paths.ModCache, mod.Guid);
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
            using var response =
                await _httpClient.GetAsync(version.Link, HttpCompletionOption.ResponseHeadersRead, cancellationToken.Token);
            response.EnsureSuccessStatusCode();

            downloadTask.TotalToDownload = response.Content.Headers.ContentLength ?? -1;

            var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken.Token);
            var fileStream = File.Create(modFilePath);

            var buffer = new byte[8192];
            float totalRead = 0;
            int bytesRead;

            var lastReportTime = DateTime.UtcNow;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken.Token);
                totalRead += bytesRead;

                var now = DateTime.UtcNow;

                if ((now - lastReportTime).TotalSeconds >= 1 || totalRead == downloadTask.TotalToDownload)
                {
                    downloadTask.Progress = totalRead / downloadTask.TotalToDownload * 100;
                    lastReportTime = now;
                }
            }

            await contentStream.FlushAsync();
            contentStream.Close();

            await fileStream.FlushAsync();
            fileStream.Close();
        }
        catch (Exception e)
        {
            downloadTask.Error = e;
            cancellationToken.Cancel();
            return downloadTask;
        }

        downloadTask.Complete = true;
        return downloadTask;
    }

    public async Task<IModTask?> RemoveModTask(IModTask task)
    {
        string guid = "";
        string name = "";

        if (task is DownloadTask downloadTask)
        {
            guid = downloadTask.ForgeMod.Guid;
            name = downloadTask.ForgeMod.Name;
        }
        else if (task is UpdateTask updateTask)
        {
            guid = updateTask.GUID;
            name = updateTask.ModName;
        }

        if (!_modDict.TryRemove(guid, out IModTask? mod))
        {
            _logger.LogError("Unable to remove mod from download Dictionary for {name}:{guid}", name,
                guid);
            return null;
        }

        return mod;
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

            if (File.Exists(Path.Join(Paths.ModCache, guid)))
            {
                File.Delete(Path.Join(Paths.ModCache, guid));
            }

            _logger.LogInformation("Mod {guid} cancelled", guid);

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

    public async Task<UpdateTask?> StartUpdateTask(ForgeModUpdate mod, CancellationTokenSource cancellationToken)
    {
        var updateTask = new UpdateTask
        {
            ModName = mod.CurrentVersion.Name,
            Version = mod.RecommendedVersion.Version,
            GUID = mod.CurrentVersion.GUID,
            Link = mod.RecommendedVersion.Link,
            Progress = 0,
            TotalToDownload = 0,
            CancellationTokenSource = cancellationToken,
            Complete = false,
            Error = null
        };

        if (!_modDict.TryAdd(updateTask.GUID, updateTask))
        {
            _modDict.Remove(updateTask.GUID, out _);
            if (!_modDict.TryAdd(updateTask.ModName, updateTask))
            {
                _logger.LogError("Something seriously went wrong adding this update task: {name}:{guid}", updateTask.ModName, updateTask.GUID);
                return null;
            }
        }

        var modFilePath = Path.Join(Paths.ModCache, updateTask.GUID);

        try
        {
            if (File.Exists(modFilePath))
            {
                File.Delete(modFilePath);
            }

            // Use a download to EFT client to test a long download
            using var response = await _httpClient.GetAsync(updateTask.Link, HttpCompletionOption.ResponseHeadersRead, cancellationToken.Token);
            response.EnsureSuccessStatusCode();

            updateTask.TotalToDownload = response.Content.Headers.ContentLength ?? -1;

            var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken.Token);
            var fileStream = File.Create(modFilePath);

            var buffer = new byte[8192];
            float totalRead = 0;
            int bytesRead;

            var lastReportTime = DateTime.UtcNow;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken.Token);
                totalRead += bytesRead;

                var now = DateTime.UtcNow;

                if ((now - lastReportTime).TotalSeconds >= 1 || totalRead == updateTask.TotalToDownload)
                {
                    updateTask.Progress = totalRead / updateTask.TotalToDownload * 100;
                    lastReportTime = now;
                }
            }

            await contentStream.FlushAsync();
            contentStream.Close();

            await fileStream.FlushAsync();
            fileStream.Close();
        }
        catch (Exception e)
        {
            updateTask.Error = e;
            cancellationToken.Cancel();
            return updateTask;
        }

        updateTask.Complete = true;
        return updateTask;
    }

    public async Task<InstallTask?> StartInstallTask(ConfigMod mod, CancellationTokenSource cancellationToken)
    {
        var installTask = new InstallTask
        {
            Mod = mod,
            CancellationTokenSource = cancellationToken,
            TotalToDownload = 0,
            Progress = 0,
            Complete = false,
            Error = null
        };

        if (!_modDict.TryAdd(installTask.Mod.GUID, installTask))
        {
            _modDict.Remove(installTask.Mod.GUID, out _);
            if (!_modDict.TryAdd(installTask.Mod.GUID, installTask))
            {
                _logger.LogError("Something seriously went wrong adding this install task: {name}:{guid}", installTask.Mod.ModName, installTask.Mod.GUID);
                return null;
            }
        }

        var modFilePath = Path.Join(Paths.ModCache, mod.GUID);
        var entries = await _sevenZip.GetEntriesAsync(modFilePath);

        // check if zip contains bepinex or spt folder for correct starting structure
        var checkForCorrectFilePath = entries.Any(x => x.ToLower().Contains("bepinex/") || x.ToLower().Contains("spt/"));

        // we checked this before, but to be sure
        if (!checkForCorrectFilePath)
        {
            _logger.LogError("Zip does not contain a bepinex or spt folder, unsupported structure, please report to SPT staff");
            installTask.Complete = false;
            installTask.Error =
                new Exception("Zip does not contain a bepinex or spt folder, unsupported structure, please report to SPT staff");
            return installTask;
        }

        await _sevenZip.ExtractToDirectoryAsync(modFilePath, _configHelper.GetConfig().GamePath);
        installTask.Complete = true;

        return installTask;
    }
}
