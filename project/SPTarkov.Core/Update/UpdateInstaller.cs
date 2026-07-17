using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace SPTarkov.Core.Update;

public enum UpdateStage
{
    Preflight,
    Downloading,
    Verifying,
    Extracting,
    Applying,
    Restarting,
}

public enum UpdateFailure
{
    None,
    AlreadyRunning,
    ModTaskInProgress,
    InsufficientSpace,
    NotWritable,
    Download,
    Verify,
    Payload,
    Apply,
    Relaunch,
}

public record UpdateProgress(UpdateStage Stage, double Percent);

public class UpdateInstaller(
    ILogger<UpdateInstaller> logger,
    UpdatePreflight preflight,
    UpdateTransaction transaction,
    SevenZip.SevenZip sevenZip
)
{
    private const int DownloadStallTimeoutMs = 60_000;

    private readonly HttpClient _httpClient = new() { Timeout = Timeout.InfiniteTimeSpan };
    private Action? _relaunch;
    private int _installActive;
    private bool _restartPending;

    /// <summary>Sets the relaunch handler: it releases the single-instance lock, starts the new build, and closes this process.</summary>
    public void SetRelaunchHandler(Action relaunch)
    {
        _relaunch = relaunch;
    }

    public async Task<UpdateFailure> InstallAsync(AvailableUpdate update, IProgress<UpdateProgress> progress, CancellationToken token)
    {
        if (Interlocked.Exchange(ref _installActive, 1) == 1)
        {
            logger.LogError("Rejecting update install: an install is already running.");
            return UpdateFailure.AlreadyRunning;
        }

        try
        {
            if (_restartPending)
            {
                logger.LogError("Rejecting update install: a committed update is waiting for a restart.");
                return UpdateFailure.Relaunch;
            }

            return await Install(update, progress, token);
        }
        finally
        {
            Interlocked.Exchange(ref _installActive, 0);
        }
    }

    private async Task<UpdateFailure> Install(AvailableUpdate update, IProgress<UpdateProgress> progress, CancellationToken token)
    {
        progress.Report(new UpdateProgress(UpdateStage.Preflight, 0));

        var preflightFailure = preflight.Check(update.Release.Asset.Size);
        if (preflightFailure != UpdateFailure.None)
        {
            logger.LogError("Update preflight failed: {Result}", preflightFailure);
            return preflightFailure;
        }

        try
        {
            transaction.Begin(update.Release.LauncherVersion);

            try
            {
                await Download(update.Release.Asset, progress, token);
            }
            catch (Exception ex)
            {
                logger.LogError("Update download failed: {Exception}", ex);
                return UpdateFailure.Download;
            }

            progress.Report(new UpdateProgress(UpdateStage.Verifying, 0));
            if (!await HashesMatch(UpdateLayout.StagingDownload, update.Release.Asset.Sha256, token))
            {
                logger.LogError("Downloaded payload failed hash verification.");
                return UpdateFailure.Verify;
            }

            progress.Report(new UpdateProgress(UpdateStage.Extracting, 0));
            if (!await ExtractPayload(token))
            {
                return UpdateFailure.Payload;
            }

            progress.Report(new UpdateProgress(UpdateStage.Applying, 0));
            transaction.Commit(update.Release.LauncherVersion);
            _restartPending = true;

            progress.Report(new UpdateProgress(UpdateStage.Restarting, 100));

            try
            {
                if (_relaunch is null)
                {
                    throw new InvalidOperationException("No relaunch handler is set.");
                }

                _relaunch();
            }
            catch (Exception ex)
            {
                logger.LogError("Relaunch after update failed: {Exception}", ex);
                return UpdateFailure.Relaunch;
            }

            return UpdateFailure.None;
        }
        catch (Exception ex)
        {
            logger.LogError("Update install failed: {Exception}", ex);
            return UpdateFailure.Apply;
        }
    }

    private async Task Download(ReleaseAsset asset, IProgress<UpdateProgress> progress, CancellationToken token)
    {
        // Cancels the transfer when no bytes arrive within the stall timeout.
        using var stall = CancellationTokenSource.CreateLinkedTokenSource(token);
        stall.CancelAfter(DownloadStallTimeoutMs);

        using var response = await _httpClient.GetAsync(asset.Url, HttpCompletionOption.ResponseHeadersRead, stall.Token);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? asset.Size;
        await using var source = await response.Content.ReadAsStreamAsync(stall.Token);
        await using var destination = File.Create(UpdateLayout.StagingDownload);

        var buffer = new byte[81920]; // CopyToAsync's default buffer size
        long read = 0;
        int count;

        while ((count = await source.ReadAsync(buffer, stall.Token)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, count), stall.Token);
            read += count;
            stall.CancelAfter(DownloadStallTimeoutMs);
            if (total > 0)
            {
                progress.Report(new UpdateProgress(UpdateStage.Downloading, read * 100.0 / total));
            }
        }
    }

    private static async Task<bool> HashesMatch(string path, string expected, CancellationToken token)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, token);
        return string.Equals(Convert.ToHexString(hash), expected, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> ExtractPayload(CancellationToken token)
    {
        var entries = await sevenZip.GetEntriesAsync(UpdateLayout.StagingDownload, token);
        if (!EntriesAllowed(entries))
        {
            return false;
        }

        Directory.CreateDirectory(UpdateLayout.StagingExtract);
        if (!await sevenZip.ExtractToDirectoryAsync(UpdateLayout.StagingDownload, UpdateLayout.StagingExtract, token))
        {
            return false;
        }

        return StagedExeExists();
    }

    private bool StagedExeExists()
    {
        if (File.Exists(Path.Combine(UpdateLayout.StagingExtract, UpdateLayout.RunningExeName)))
        {
            return true;
        }

        logger.LogError("Rejecting update payload: no {Exe} in the archive.", UpdateLayout.RunningExeName);
        return false;
    }

    private bool EntriesAllowed(List<string> entries)
    {
        var rejected = UpdatePayload.DisallowedEntries(entries);

        foreach (var entry in rejected)
        {
            logger.LogError("Rejecting update payload: entry not allowed (\"{Entry}\").", entry);
        }

        return rejected.Count == 0;
    }
}
