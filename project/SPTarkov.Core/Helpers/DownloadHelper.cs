using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Forge;

namespace SPTarkov.Core.Helpers;

public class DownloadHelper
{
    private ILogger<DownloadHelper> _logger;
    private HttpClient _httpClient;
    private ConcurrentDictionary<string, ForgeDownloadTask> _downloadDict = new();

    public DownloadHelper
    (
        ILogger<DownloadHelper> logger
    )
    {
        _logger = logger;

        // leaving default atm, this will be making requests to unknown servers.
        var handler = new HttpClientHandler();
        handler.UseCookies = false;
        _httpClient = new HttpClient(handler);
    }

    public bool AddDownloadTask(string modName, string url, CancellationTokenSource token)
    {
        var taskToAdd = new ForgeDownloadTask(modName, url, token, _httpClient);
        if (!_downloadDict.TryAdd(modName, taskToAdd))
        {
            _logger.LogWarning("Download with modName: {modName} already exists", modName);
            return false;
        }

        Task.Factory.StartNew(taskToAdd.Start);
        return true;
    }

    public async Task<bool> CancelDownloadTask(string modName)
    {
        if (!_downloadDict.TryRemove(modName, out var value))
        {
            _logger.LogWarning("Download with modName {modName} did not exist in Dict", modName);
            return false;
        }

        return await value.Cancel();
    }

    public bool CloseDownloadTask(string modName)
    {
        if (!_downloadDict.TryRemove(modName, out _))
        {
            _logger.LogWarning("Download with modName {modName} did not exist in Dict", modName);
            return false;
        }

        return true;
    }

    public ConcurrentDictionary<string, ForgeDownloadTask> GetDownloadTasks()
    {
        return _downloadDict;
    }

    public ForgeDownloadTask? GetDownloadTask(string modName)
    {
        if (!_downloadDict.TryGetValue(modName, out var value))
        {
            _logger.LogWarning("Download with modName: {modName} did not exist in Dict", modName);
            return null;
        }

        return value;
    }
}
