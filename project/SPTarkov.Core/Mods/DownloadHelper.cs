using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Forge;
using SPTarkov.Core.SPT;

namespace SPTarkov.Core.Mods;

public class DownloadHelper
{
    private ILogger<DownloadHelper> _logger;
    private HttpClient _httpClient;
    private ConcurrentDictionary<string, DownloadTask> _downloadDict = new();

    public DownloadHelper(
        ILogger<DownloadHelper> logger
    )
    {
        _logger = logger;

        // leaving default atm, this will be making requests to unknown servers.
        var handler = new HttpClientHandler();
        handler.UseCookies = false;
        _httpClient = new HttpClient(handler);
    }

    public async Task<DownloadTask> StartDownloadTask(ForgeBase mod, ForgeModVersion version, CancellationTokenSource cancellationToken)
    {
        var downloadTask = new DownloadTask
        {
            ForgeMod = mod,
            Version = version,
            TotalToDownload = 0,
            Progress = 0,
            CancellationToken = cancellationToken,
            Complete = false,
            Error = null
        };

        if (!_downloadDict.TryAdd(mod.Guid, downloadTask))
        {
            _logger.LogError("Unable to add download task for {name}:{guid}", mod.Name, mod.Guid);
        }

        var modFilePath = Path.Combine(Paths.ModCache, mod.Guid);
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

    public async Task<DownloadTask?> RemoveDownloadTask(DownloadTask downloadTask)
    {
        if (!_downloadDict.TryRemove(downloadTask.ForgeMod.Guid, out DownloadTask? mod))
        {
            _logger.LogError("Unable to remove mod from download Dictionary for {name}:{guid}", downloadTask.ForgeMod.Name,
                downloadTask.ForgeMod.Guid);
            return null;
        }

        return mod;
    }

    public async Task<bool> CancelModDownload(string guid)
    {
        try
        {
            if (!_downloadDict.TryRemove(guid, out DownloadTask? downloadTask))
            {
                _logger.LogError("Couldn't remove download task for {guid}", guid);
                return false;
            }

            await downloadTask.CancellationToken.CancelAsync();

            if (File.Exists(Path.Combine(Paths.ModCache, guid)))
            {
                File.Delete(Path.Combine(Paths.ModCache, guid));
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

    public ConcurrentDictionary<string, DownloadTask> GetDownloadTasks()
    {
        return _downloadDict;
    }

    public Task<UpdateTask?> UpdateMod(string guid, string version)
    {







        var updateTask = new UpdateTask
        {
            ModName = null,
            Version = null,
            GUID = null,
            Link = null,
            Progress = 0,
            TotalToDownload = 0,
            CancellationToken = null,
            Complete = false,
            Error = null
        }
    }
}
