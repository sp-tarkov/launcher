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
    private readonly ConcurrentDictionary<string, IModTask> _modDict = new();
    private readonly SevenZip.SevenZip _sevenZip;

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
        var handler = new HttpClientHandler
        {
            UseCookies = false
        };

        _httpClient = new HttpClient(handler);
    }

    public async Task<DownloadTask?> StartDownloadTask(ForgeBase mod, ForgeModVersion version, CancellationTokenSource cancellationTokenSource)
    {
        var downloadTask = new DownloadTask
        {
            ForgeMod = mod,
            Version = version,
            TotalToDownload = 0,
            Progress = 0,
            CancellationTokenSource = cancellationTokenSource,
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
                await _httpClient.GetAsync(version.Link, HttpCompletionOption.ResponseHeadersRead, downloadTask.CancellationTokenSource.Token);
            response.EnsureSuccessStatusCode();

            downloadTask.TotalToDownload = response.Content.Headers.ContentLength ?? -1;

            var contentStream = await response.Content.ReadAsStreamAsync(downloadTask.CancellationTokenSource.Token);
            var fileStream = File.Create(modFilePath);

            var buffer = new byte[8192];
            float totalRead = 0;
            int bytesRead;

            var lastReportTime = DateTime.UtcNow;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, downloadTask.CancellationTokenSource.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, downloadTask.CancellationTokenSource.Token);
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
            await downloadTask.CancellationTokenSource.CancelAsync();
            return downloadTask;
        }

        downloadTask.Complete = true;
        return downloadTask;
    }

    public void RemoveModTask(IModTask task)
    {
        var guid = "";
        var name = "";

        switch (task)
        {
            case DownloadTask downloadTask:
                guid = downloadTask.ForgeMod.Guid;
                name = downloadTask.ForgeMod.Name;
                break;
            case UpdateTask updateTask:
                guid = updateTask.GUID;
                name = updateTask.ModName;
                break;
            case InstallTask installTask:
                guid = installTask.Mod.GUID;
                name = installTask.Mod.ModName;
                break;
        }

        if (!_modDict.TryRemove(guid, out _))
        {
            _logger.LogError("Unable to remove mod from download Dictionary for {name}:{guid}", name,
                guid);
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

    public async Task<UpdateTask?> StartUpdateTask(ForgeModUpdate mod, CancellationTokenSource cancellationTokenSource)
    {
        var updateTask = new UpdateTask
        {
            ModName = mod.CurrentVersion.Name!,
            Version = mod.RecommendedVersion.Version!,
            GUID = mod.CurrentVersion.GUID!,
            Link = mod.RecommendedVersion.Link!,
            Progress = 0,
            TotalToDownload = 0,
            CancellationTokenSource = cancellationTokenSource,
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
            using var response = await _httpClient.GetAsync(updateTask.Link, HttpCompletionOption.ResponseHeadersRead, updateTask.CancellationTokenSource.Token);
            response.EnsureSuccessStatusCode();

            updateTask.TotalToDownload = response.Content.Headers.ContentLength ?? -1;

            var contentStream = await response.Content.ReadAsStreamAsync(updateTask.CancellationTokenSource.Token);
            var fileStream = File.Create(modFilePath);

            var buffer = new byte[8192];
            float totalRead = 0;
            int bytesRead;

            var lastReportTime = DateTime.UtcNow;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, updateTask.CancellationTokenSource.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, updateTask.CancellationTokenSource.Token);
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
            await updateTask.CancellationTokenSource.CancelAsync();
            return updateTask;
        }

        updateTask.Complete = true;
        return updateTask;
    }

    public async Task<InstallTask?> StartInstallTask(ConfigMod mod, CancellationTokenSource cancellationTokenSource)
    {
        var installTask = new InstallTask
        {
            Mod = mod,
            CancellationTokenSource = cancellationTokenSource,
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
        var entries = await _sevenZip.GetEntriesAsync(modFilePath, installTask.CancellationTokenSource.Token);

        // check if zip contains bepinex or spt folder for correct starting structure
        // this should be bepinex\ on windows and bepinex/ on linux
        var checkForCorrectFilePath = entries.Any(x => x.ToLower().Contains("bepinex" + Path.DirectorySeparatorChar) || x.ToLower().Contains("spt" + Path.DirectorySeparatorChar));

        // we checked this before, but to be sure
        if (!checkForCorrectFilePath)
        {
            _logger.LogError("Zip does not contain a bepinex or spt folder, unsupported structure, please report to SPT staff");
            installTask.Complete = false;
            installTask.Error =
                new Exception("Zip does not contain a bepinex or spt folder, unsupported structure, please report to SPT staff");
            return installTask;
        }

        await _sevenZip.ExtractToDirectoryAsync(modFilePath, _configHelper.GetConfig().GamePath, installTask.CancellationTokenSource.Token);
        installTask.Complete = true;

        return installTask;
    }
}
