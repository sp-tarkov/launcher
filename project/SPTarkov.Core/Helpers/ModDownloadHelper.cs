using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SPTarkov.Core.Forge;

namespace SPTarkov.Core.Helpers;

public class ModDownloadHelper
{
    // private ILogger<ModDownloadHelper> _logger;
    private HttpClient _httpClient;
    private ConcurrentDictionary<string, ForgeDownloadTask> _downloadDict = new();

    public ModDownloadHelper
    (
        // ILogger<ModDownloadHelper> logger
    )
    {
        // _logger = logger;

        // leaving default atm, this will be making requests to unknown servers.
        var handler = new HttpClientHandler();
        handler.UseCookies = false;
        _httpClient = new HttpClient(handler);
    }

    public bool AddDownloadTask(string modName, string url, CancellationTokenSource token, IProgress<float>? progress = null)
    {
        var taskToAdd = new ForgeDownloadTask(modName, url, token, _httpClient, progress);
        if (!_downloadDict.TryAdd(modName, taskToAdd))
        {
            // _logger.LogWarning("Download with key: {key} already exists", key);
            return false;
        }

        return true;
    }

    public async Task<bool> CancelDownloadTask(string key)
    {
        if (!_downloadDict.TryRemove(key, out var value))
        {
            // _logger.LogWarning("Download with key {key} did not exist in Dict", key);
            return false;
        }

        await value.CancellationToken.CancelAsync();
        return true;
    }

    public ForgeDownloadTask? GetDownloadTask(string key)
    {
        if (!_downloadDict.TryGetValue(key, out var value))
        {
            // _logger.LogWarning("Download with Key: {key} did not exist in Dict", key);
            return null;
        }

        return value;
    }
}
